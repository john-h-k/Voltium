namespace Voltium.Common.Threading
{
    internal interface IValueLock
    {
        public void Enter(ref bool taken);
        public void Exit();
    }
}
