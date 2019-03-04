using UnityEngine;

namespace ECS_MLAgents_v0.Core
{
    public interface IDecisionRequester
    {
        bool Ready { get; }

        void Update();

        void Reset();
    }
    
    
    public class FixedCountRequester : IDecisionRequester
    {
        private int _counter;
        private int _interval;

        public FixedCountRequester(int interval = 0)
        {
            _interval = interval;
        }
        
        public bool Ready
        {
            get { return _counter >= _interval; }
        }

        public void Update()
        {
            _counter += 1;
        }

        public void Reset()
        {
            _counter = 0;
        }
    }
    
    public class FixedTimeRequester : IDecisionRequester
    {
        private float _counter;
        private float _interval;

        public FixedTimeRequester(float interval = 0)
        {
            _interval = interval;
        }
        
        public bool Ready
        {
            get { return _counter >= _interval; }
        }

        public void Update()
        {
            _counter += Time.deltaTime;
        }

        public void Reset()
        {
            _counter = 0;
        }
    }

    public class ManualRequester : IDecisionRequester
    {
        private bool _ready;
        
        public bool Ready
        {
            get { return _ready; }
        }
        
        public void Update()
        {
        }

        public void Reset()
        {
            _ready = false;
        }
        
    }
    
    
}
