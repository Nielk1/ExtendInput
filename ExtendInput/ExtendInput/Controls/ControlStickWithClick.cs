using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public interface IControlStickWithClick : IControl
    {
        float X { get; set; }
        float Y { get; set; }
        bool Click { get; set; }
    }
    public class ControlStickWithClick : ControlStick, IControlStickWithClick, IControlStick
    {
        public bool Click { get; set; }

        public ControlStickWithClick() { }
        
        public override T Value<T>(string key)
        {
            switch (key)
            {
                case "click":
                    return (T)Convert.ChangeType(Click, typeof(T));
                default:
                    return base.Value<T>(key);
            }
        }
        public override Type Type(string key)
        {
            switch (key)
            {
                case "click":
                    return typeof(bool);
                default:
                    return base.Type(key);
            }
        }

        public override object Clone()
        {
            ControlStickWithClick newData = new ControlStickWithClick();

            newData.X = this.X;
            newData.Y = this.Y;
            newData.Click = this.Click;

            return newData;
        }
    }
}
