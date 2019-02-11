using Barracuda;
using ECS_MLAgents_v0.Core.Inference;
using Unity.Collections;
using Unity.Jobs;

namespace ECS_MLAgents_v0.Core
{
    /// <summary>
    /// This class uses a pretrained Neural Network model to take the decisions for a batch of
    /// agents. As such, it implements a IAgentDecision interface and requires a Barracuda Neural
    /// Network model as input during construction.
    /// </summary>
    public class NNDecision : IAgentDecision
    {
        private NNModel _model; 
        public InferenceDevice inferenceDevice = InferenceDevice.CPU;
        private Model _barracudaModel;
        private IWorker _engine;
        private const bool _verbose = false;

        private float[] sensorData = new float[0];
        /// <summary>
        /// Generates a new NNDecision object that uses the model input to take a decision for
        /// the agents present in the batches.
        /// </summary>
        /// <param name="model"> The Barracuda NNModel that will be use for the decision</param>
        public NNDecision(NNModel model)
        {
            _model = model;
            D.logEnabled = _verbose;
            _engine?.Dispose();
                
            _barracudaModel = ModelLoader.Load(model.Value);
            var executionDevice = inferenceDevice == InferenceDevice.GPU
                ? BarracudaWorkerFactory.Type.ComputeFast
                : BarracudaWorkerFactory.Type.CSharpFast;
                                       
            _engine = BarracudaWorkerFactory.CreateWorker(
                executionDevice, _barracudaModel, _verbose);
            
        }
        
        public JobHandle DecideBatch(ref NativeArray<float> sensor,
            ref NativeArray<float> actuator,
            int sensorSize,
            int actuatorSize, 
            int nAgents,
            JobHandle handle)
        {
            if (sensorData.Length < sensor.Length)
            {
                sensorData = new float[sensor.Length];
            }
            
            sensor.CopyTo(sensorData);
            // TODO : This is additional allocation here... need to go FASTER !
            var sensorT = new Tensor(
                new TensorShape(nAgents, sensorSize),
                sensorData,
                "sensor");
            
            _engine.Execute(sensorT);
            sensorT.Dispose();
            var actuatorT = _engine.Fetch("actuator");

            actuator.Slice(
                0, actuatorSize*nAgents).CopyFrom(actuatorT.data.Download(actuator.Length));
            actuatorT.Dispose();
            sensorT.Dispose();
            
            return handle;
        }

    }
}
