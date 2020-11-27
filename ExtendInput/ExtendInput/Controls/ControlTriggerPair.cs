using System;

namespace ExtendInput.Controls
{
    public class ControlTriggerPair : IControl, IControlPair<ControlTrigger>
    {
        public bool HasStage2 { get; private set; }
        public ControlTrigger Left { get; set; }
        public ControlTrigger Right { get; set; }

        public ControlTriggerPair(bool HasStage2)
        {
            this.HasStage2 = HasStage2;
            Left = new ControlTrigger(HasStage2);
            Right = new ControlTrigger(HasStage2);
        }
        public T Value<T>(string key)
        {
            string[] split = key.Split(new char[] { ':' }, 2);
            switch (split[0])
            {
                case "l":
                    return Left.Value<T>(split[1]);
                case "r":
                    return Right.Value<T>(split[1]);
                default:
                    return default;
            }
        }
        public Type Type(string key)
        {
            string[] split = key.Split(new char[] { ':' }, 2);
            switch (split[0])
            {
                case "l":
                    return Left.Type(split[1]);
                case "r":
                    return Right.Type(split[1]);
                default:
                    return default;
            }
        }
        public object Clone()
        {
            ControlTriggerPair newData = new ControlTriggerPair(this.HasStage2);

            newData.Left = (ControlTrigger)this.Left.Clone();
            newData.Right = (ControlTrigger)this.Right.Clone();

            return newData;
        }
    }
}
