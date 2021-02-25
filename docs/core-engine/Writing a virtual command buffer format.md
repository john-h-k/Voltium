# Writing a virtual command buffer format

Instead of immediately calling native APIs when GPU commands are recorded, Voltium builds a custom encoded command buffer that records API-agnostic graphics/compute commands to memory.

## Stream design

The design priorities of the stream are:

* Ease of parsing
* Memory efficiency
* Must be strongly API agnostic
* Performant to encode/decode

The most intuitive design to a C# developer might be something like:

```cs
abstract class Command;

sealed class DrawCommand : Command;
sealed class BeginRenderPassCommand : Command;
sealed class DispatchCommand : Command;
// etc
```

and using a `Command[]` to encode the commands. However, this has a few major drawbacks:

* Unfriendly to serialization
* Unfriendly to caches - constant type checks and constant cache thrashing across objects
* Allocation heavy (although a command cache can alleviate this somewhat)

Using an enum to represent each command type, and then a struct which can be any command, results in the most cache-friendly parsing pattern.
Having a struct which just contains every command is infeasible due to the size of the struct, so a union is required. However, this still has ineffeciency. Consider an `EndRenderPass` command.
This needs no parameters at all, so why should it take as much space as a `BeginRenderPass` command which takes a large amount of metadata? To solve this issue, commands in the buffer are variable size.
Each command can either be:

* Empty - no parameters. The next command is simply retrieved by offsetting the pointer by the size of the enum
* Fixed size, e.g a draw. The next command is retrieved by offsetting the pointer by the size of the enum and the size of the parameter struct
* Variable size, e.g a texture clear (which can take 0->n rectangles). The next command is retrieved by offsetting the point by the size of the enum, the size of the struct, and the number of tail elements (which is stored in the parameters) multiplied by the size of the tail element

The command format is entirely API agnostic, and serializable. Due to the variable-length nature of the encoding, command types must be unmanaged (as the GC cannot understand the offsets of object refs in the buffer). Most objects (pipeline state objects, buffers, textures, acceleration structures, query heaps) are represented by strongly-typed opaque `uint64` handles.

## Motivations for this approach

This design meets all the design goals I wanted for Voltium's command API. Principally:

* A single type for command recording (API agnostic)
* Efficient, very performant, minimal overhead recording with no sync primitives/syscalls
* Allows easy debugging and replay of command buffers
* Serialization/deserialization of command buffers
* Allows buffer optimisation and analysis
* Allows constant best-case command allocator behaviour

Using these formats also brings in other implicit advantages:

* Other frontends - any given engine frontend can utilise the Voltium backend, it just needs to have a shim to emit the right command buffer
* Debugging/profiling - the entire command buffer can be decoded and printed, analyzed, and saved for later
* Decoupling - command recording is no longer API-dependant

## Encoding a command buffer

Encoding a command buffer must be fast and non-complex. There are 3 primary types involved in encoding a command buffer:

* `ContextEncoder` - for emitting the commands to memory
* `CommandType` - for indicating which type you emit
* `ICommand` - the base interface for each command

```cs
interface ICommand
{
    CommandType Command;
}
```

`ContextEncoder` allows you to emit commands in 3 manners:

```cs
void EmitEmpty(CommandType command);
```

This method is used to emit commands with no parameteres, and simply writes it to the stream and advances.

```cs
void Emit<TCommand>(TCommand* pCommand) where TCommand : unmanaged, ICommand;
```

This method is used to emit a fixed-size command, such as a draw:

```cs
var commandDraw = new CommandDraw
{
    IndexCountPerInstance = indexCountPerInstance,
    InstanceCount = instanceCount,
    StartIndexLocation = startIndexLocation,
    BaseVertexLocation = baseVertexLocation,
    StartInstanceLocation = startInstanceLocation,
}

_contextEncoder.Emit(&commandDraw); // CommandType is inferred from the ICommand interface
```

```cs
void EmitVariable<TCommand, TVariable>(TCommand* pCommand, TVariable* pVariable, uint variableCount) where TCommand : unmanaged, ICommand where TVariable : unmanaged;
void EmitVariable<TCommand, TVariable1, TVariable2>(TCommand* pCommand, TVariable1* pVariable1, TVariable2* pVariable2, uint variableCount) where TCommand : unmanaged, ICommand where TVariable1 : unmanaged where TVariable2 : unmanaged;
```

These methods allow you to emit variable-length commands, such as binding 32 bit constants to the pipeline:

```cs
uint* pConstants = stackalloc uint[] { 1, 2, 3, };

var command = new CommandBind32BitConstants
{
    BindPoint = BindPoint.Graphics,
    OffsetIn32BitValues = 0,
    ParameterIndex = 1,
    Num32BitValues = 3
};

_contextEncoder.EmitVariable(&command, pConstants, command.Num32BitValues);
```

## Decoding a command buffer

Decoding a command buffer is designed to be a simple serial iterative process (it is explicitly not designed for parallel decoding).
The `ContextDecoder` type provides `AdvanceEmptyCommand`/`AdvanceCommand`/`AdvanceVariableCommand` which map to the above `Emit` methods, but advance the pointer instead of emitting anything.
It is then simply a case of switching on the command type and emitting the appropriate native commands or validation. A command decoder is not permitted to modify the buffer, as the buffers are resubmittable, and the underlying memory is reused when reset.

## Optimisations

This format allows a variety of optimisations to take place on the buffer format, including, but not limited to:

* Barrier merging/elimination
* Redundant state change elimination
* Minimal native command allocation

The expected use of the serialisation is that a large set of buffers will be serialized at once (and it is likely they were recorded in parallel), in preperation for a single call to
`ID3D12CommandQueue::ExecuteCommandLists` or `vkQueueSubmit` - so the buffers will have been recorded without knowledge of each other, but are submitted together. This allows us to perform optimisations
the individual buffers can't. Exciting! 😃

A key one is redundant state-change elimination. We can notice if calls to `SetPipeline`/`SetBlendFactor` etc don't actually change the set state across a command boundary (or within one, but
that is less likely to occur), and simply not emit said calls to the native API.

We can also merge and eliminate barriers. The Voltium barrier API is "closest" to the D3D12 one, where pipeline stages + image layouts are combined into the idea of a "resource state"
(e.g, `PixelShaderResource` is the same image layout as `NonPixelShaderResource`, but a later stage of the pipeline). When we are given a set of barriers in a command buffer, or across a buffer,
we can notice this (by using the `PeekCommand` methods) and attempt to merge the barriers by picking the minimum set from the barriers that achieves the consumer's desires.

Normally, a decent amount of work goes into making sure your command allocators are running ideally, which consists of 2 approaches:

* Make sure they are "warmed-up" and not constantly re-allocating
* Make sure they are reused for similar sized passes/lists so they are as tightly sized as possible

The virtual command buffer approach eliminates this, because only a single allocator is required and it only grows when a submission is larger than before (which also means it can be trivially reset).
The actual buffer the virtual commands are recorded into uses managed GC memory from an `ArrayPool<byte>`.

## Future optimisations

* PGO
* Command reordering
* Work reodering

PGO (Profiling guided optimisation) - by recording hueristics about various passes/lists (via things like decoder-inserted timing queries), we can make optimisation/submission decisions about them.

Command reordering is switching around state settings/copies/draws/dispatches, and merging them, similar to how the GPU and driver naturally will due to the pipelined nature of a GPU, but with the additional information our decoder has. We can merge copies and transform barriers into split barriers, etc.

Work reordering is a coarser-grained version of command reordering, which more closely relates to pass reordering/culling as done by our render graph. Currently it is not a primary goal of the virtual buffer decoder, but it could well be something to explore. We can create keys about each draw/dispatch based on state (pipeline, bound resources, etc) and sort via these keys (respecting dependencies) to minimise state thrashing on the GPU.
