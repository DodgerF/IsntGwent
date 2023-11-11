namespace MyEventBus
{
    public class CallbackWithPriority
    {
        /// <summary>
        /// The higher the priority, the sooner it will be invoked.
        /// </summary>
        public readonly int Priority;
        public readonly object Callback;

        public CallbackWithPriority(int priority, object callback)
        {
            Priority = priority;
            Callback = callback;
        }
    }
}
