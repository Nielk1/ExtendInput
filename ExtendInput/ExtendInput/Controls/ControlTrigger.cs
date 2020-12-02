using System;

namespace ExtendInput.Controls
{
    public class ControlTrigger : IControl
    {
        public bool HasStage2 { get; private set; }
        public float Analog { get; private set; }
        public bool Stage2 { get; private set; }
        public float? PendingAnalog { get; set; }
        public bool? PendingStage2 { get; set; }

        public ControlTrigger(bool HasStage2)
        {
            this.HasStage2 = HasStage2;
        }
        public T Value<T>(string key)
        {
            switch (key)
            {
                case "analog":
                    return (T)Convert.ChangeType(Analog, typeof(T));
                case "stage2":
                    return (T)Convert.ChangeType(Stage2, typeof(T));
                default:
                    return default;
            }
        }
        public Type Type(string key)
        {
            switch (key)
            {
                case "analog":
                    return typeof(float);
                case "stage2":
                    return typeof(bool);
                default:
                    return default;
            }
        }
        public object Clone()
        {
            ControlTrigger newData = new ControlTrigger(this.HasStage2);

            newData.Analog = this.Analog;

            newData.Stage2 = this.Stage2;

            return newData;
        }

        public void ProcessPendingInputs()
        {
            Analog = PendingAnalog ?? Analog;
            Stage2 = PendingStage2 ?? Stage2;
            PendingAnalog = null;
            PendingStage2 = null;
        }
    }
}
