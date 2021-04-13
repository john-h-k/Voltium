using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Toolkit.HighPerformance.Extensions;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.RenderEngine;
using Voltium.RenderEngine.Passes;

using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.RenderEngine
{
    public unsafe sealed partial class RenderGraph
    {
        public Builder CreatePassBuilder()
        {
            return new Builder(dummy: true);
        }

        /// <summary>
        /// The type used in a <see cref="RenderGraph"/> to register pass dependencies and resources
        /// </summary>
        public struct Builder
        {
            private ValueList<(ResourceHandle Resource, ResourceState State, ResourceState FinalState)> _transitions;
            private PassRegisterDecision _decision;

            internal ValueList<(ResourceHandle Resource, ResourceState State, ResourceState FinalState)> Transitions => _transitions;
            internal PassRegisterDecision Decision => _decision;

            internal Builder(bool dummy)
            {
                _transitions = ValueList<(ResourceHandle Resource, ResourceState State, ResourceState FinalState)>.Create();
                _decision = PassRegisterDecision.ExecutePass;
            }

            /// <inheritdoc cref="PassRegisterDecision.AsyncComputeValid"/>
            public void EnableAsyncCompute() => _decision |= PassRegisterDecision.AsyncComputeValid;

            /// <inheritdoc cref="PassRegisterDecision.RemovePass"/>
            public void CullPass() => _decision = PassRegisterDecision.RemovePass;

            /// <inheritdoc cref="PassRegisterDecision.HasExternalOutputs"/>
            public void HasExternalOutput() => _decision |= PassRegisterDecision.HasExternalOutputs;

            /// <summary>
            /// Indicates a pass uses a resource in a certain manner
            /// </summary>
            /// <param name="buffer">The resource handle</param>
            /// <param name="flags">The <see cref="ResourceState"/> the pass uses it as</param>
            /// <param name="finalState">The <see cref="ResourceState"/> the pass leaves the resource in.
            /// If this is <see langword="null"/>, it is the same as setting it to the same as <paramref name="flags"/></param>
            public void MarkUsage(BufferHandle buffer, ResourceState flags, ResourceState? finalState = null)
                => _transitions.Add((buffer.AsResourceHandle(), flags, finalState ?? flags));

            /// <summary>
            /// Indicates a pass uses a resource in a certain manner
            /// </summary>
            /// <param name="texture">The resource handle</param>
            /// <param name="flags">The <see cref="ResourceState"/> the pass uses it as</param>
            /// <param name="finalState">The <see cref="ResourceState"/> the pass leaves the resource in.
            /// If this is <see langword="null"/>, it is the same as setting it to the same as <paramref name="flags"/></param>
            public void MarkUsage(TextureHandle texture, ResourceState flags, ResourceState? finalState = null)
                => _transitions.Add((texture.AsResourceHandle(), flags, finalState ?? flags));
        }
    }
}
