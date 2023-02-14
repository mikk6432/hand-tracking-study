using UnityEngine;
using UnityEngine.Events;

namespace Utils
{
    public class WaitForEvent: CustomYieldInstruction
    {
        private bool _happened;
        public override bool keepWaiting => !_happened;

        public WaitForEvent(UnityEvent targetEvent)
        {
            UnityAction callback = null;
            callback = () =>
            {
                _happened = true;

                targetEvent.RemoveListener(callback);
            };
            
            targetEvent.AddListener(callback);
        }
    }
    
    public class WaitForEvent<T>: CustomYieldInstruction
    {
        private bool _happened;
        public T Data { get; private set; }
        public override bool keepWaiting => !_happened;

        public WaitForEvent(UnityEvent<T> targetEvent)
        {
            UnityAction<T> callback = null;
            callback = data =>
            {
                _happened = true;
                Data = data;
                
                targetEvent.RemoveListener(callback);
            };
            
            targetEvent.AddListener(callback);
        }
    }
}