using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Xna.Framework.Graphics
{
    public class EffectParameterCollection : IEnumerable<EffectParameter>
    {
        internal Dictionary<string, EffectParameter> _parameters = new Dictionary<string, EffectParameter>();
        List<string> names = new List<string>();

        public EffectParameter this[int index]
        {
            get
            {
                return _parameters[names[index]];
            }
            set
            {
                this[value.Name] = value;
                names.Insert(index, value.Name);
            }
        }

        public int Count
        {
            get { return _parameters.Count; }
        }

        public EffectParameter this[string name]
        {
            get
            {
                return _parameters[name];
            }
            set 
            {
                _parameters[name] = value;
                names.Insert(value.internalIndex, value.Name);
            }
        }

        public IEnumerator<EffectParameter> GetEnumerator()
        {
            return _parameters.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _parameters.Values.GetEnumerator();
        }
    }
}
