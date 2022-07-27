using ExtendInput.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput
{
    public class ControlConverter
    {
        private static object InstanceLock = new object();
        private static ControlConverter instance;
        public static ControlConverter Instance
        {
            get
            {
                lock (InstanceLock)
                {
                    if (instance == null)
                        instance = new ControlConverter();
                    return instance;
                }
            }
        }

        private Dictionary<Type, Dictionary<Type, IControlConverter>> ConvertersFromTo;
        private Dictionary<Type, Dictionary<Type, IControlConverter>> ConvertersToFrom;
        private ControlConverter()
        {
            ConvertersFromTo = new Dictionary<Type, Dictionary<Type, IControlConverter>>();
            ConvertersToFrom = new Dictionary<Type, Dictionary<Type, IControlConverter>>();

            foreach (Type controlType in typeof(IControl).GetTypeInfo().Assembly.GetTypes()) // find all Control classes via the fact they implement IControl
            {
                // This code might work better by scanning implicit or explicit cast implementation
                if (controlType.IsClass && controlType.GetInterfaces().Contains(typeof(IControl))) // ensure the target is a class and has the IControl interface
                {
                    foreach (Type controlInterface in controlType.GetInterfaces()) // scan the interfaces on the class
                    {
                        if(controlInterface.IsInterface && controlInterface.GetInterfaces().Contains(typeof(IControl))) // if the found interface also implements IControl
                        {
                            Console.WriteLine($"{controlType} can convert to {controlInterface} via interface tree"); // save this as an implicit conversion via saved interface
                            if (!ConvertersFromTo.ContainsKey(controlType))
                                ConvertersFromTo[controlType] = new Dictionary<Type, IControlConverter>();
                            if (!ConvertersToFrom.ContainsKey(controlInterface))
                                ConvertersToFrom[controlInterface] = new Dictionary<Type, IControlConverter>();

                            Type converterType = typeof(BasicControlConverter<,>).MakeGenericType(controlType, controlInterface);
                            IControlConverter plugin = (IControlConverter)Activator.CreateInstance(converterType);
                            ConvertersFromTo[controlType][controlInterface] = plugin;
                            ConvertersToFrom[controlInterface][controlType] = plugin;
                        }
                    }
                }
                // scan for adapaters that register to convert an IControl implementer to an interface it's not normally compatiable with
                // the only reason for this is so that plugins in the future can add converters without needing to implement implicit or explicit casts into the control classes
                if (controlType.IsClass && controlType.GetInterfaces().Contains(typeof(IControlConverter)))
                {
                    ControlConverterAttribute attr = controlType.GetCustomAttribute<ControlConverterAttribute>();
                    if(attr != null)
                    {
                        Console.WriteLine($"{attr.Base} can convert to {attr.Dest} via {controlType}");
                        if (!ConvertersFromTo.ContainsKey(attr.Base))
                            ConvertersFromTo[attr.Base] = new Dictionary<Type, IControlConverter>();
                        if (!ConvertersToFrom.ContainsKey(attr.Dest))
                            ConvertersToFrom[attr.Dest] = new Dictionary<Type, IControlConverter>();

                        IControlConverter plugin = (IControlConverter)Activator.CreateInstance(controlType);
                        ConvertersFromTo[attr.Base][attr.Dest] = plugin;
                        ConvertersToFrom[attr.Dest][attr.Base] = plugin;
                    }
                }
            }
        }

        public List<Type> GetConvertList<Base>() where Base : IControl
        {
            if (!ConvertersFromTo.ContainsKey(typeof(Base))) return null;
            return ConvertersFromTo[typeof(Base)].Keys.ToList();
        }

        public List<Type> GetConvertList(Type ControlType)
        {
            if (!ConvertersFromTo.ContainsKey(ControlType)) return null;
            return ConvertersFromTo[ControlType].Keys.ToList();
        }

        public bool? CanConvert<Base, Dest>() where Base : IControl
                                              where Dest : IControl
        {
            if (!ConvertersFromTo.ContainsKey(typeof(Base))) return false;
            if (!ConvertersFromTo[typeof(Base)].ContainsKey(typeof(Dest))) return false;
            if (ConvertersFromTo[typeof(Base)][typeof(Dest)].CanAlwaysConvert) return true;
            return null;
        }
        public bool CanConvert<Base, Dest>(Base Source) where Base : IControl
                                                        where Dest : IControl
        {
            return CanConvert<Base, Dest>() ?? ConvertersFromTo[typeof(Base)][typeof(Dest)].CanConvert(Source);
        }

        public Dest Convert<Base, Dest>(Base Source) where Base : IControl
                                                     where Dest : IControl
        {
            if (!ConvertersFromTo.ContainsKey(typeof(Base))) return default(Dest);
            if (!ConvertersFromTo[typeof(Base)].ContainsKey(typeof(Dest))) return default(Dest);
            //return (Dest)System.Convert.ChangeType(ConvertersFromTo[typeof(Base)][typeof(Dest)].Convert(Source), typeof(Dest));
            return (Dest)ConvertersFromTo[typeof(Base)][typeof(Dest)].Convert(Source);
        }
    }

    public interface IControlConverter
    {
        bool CanAlwaysConvert { get; }
        bool CanConvert(IControl Control);
        IControl Convert(IControl Control);
    }
    public class BasicControlConverter<Base, Dest> : IControlConverter where Base : IControl
                                                                       where Dest : IControl
    {
        public bool CanAlwaysConvert => true;
        public bool CanConvert(IControl Control) => true;
        public IControl Convert(IControl Control)
        {
            //return (Dest)System.Convert.ChangeType(Control, typeof(Dest));
            return Control;
        }
    }
    public class ControlConverterAttribute : Attribute
    {
        public Type Base;
        public Type Dest;
        public ControlConverterAttribute(Type Base, Type Dest)
        {
            this.Base = Base;
            this.Dest = Dest;
        }
    }
}