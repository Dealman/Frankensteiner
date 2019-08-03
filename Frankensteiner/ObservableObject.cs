using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frankensteiner
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                this.PropertyChanged(this, e);
            }
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public virtual void VerifyPropertyName(string propertyName)
        {
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid Property Name: " + propertyName;
                if (this.ThrowOnInvalidPropertyName)
                {
                    throw new Exception(msg);
                }
                else
                {
                    Debug.Fail(msg);
                }
            }
        }
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }
    }
}
