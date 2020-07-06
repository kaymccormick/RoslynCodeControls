using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace RoslynCodeControls
{
    public class CustomTextSource4Proxy : INotifyPropertyChanged
    {
        private RoslynCodeControl _codeControl;
        private CustomTextSource4 _customTextSource;

        public CustomTextSource4Proxy(RoslynCodeControl codeControl)
        {
            CodeControl = codeControl;
            
        }

        public RoslynCodeControl CodeControl
        {
            get { return _codeControl; }
            set
            {
                if (Equals(value, _codeControl))
                    return;
                if (_codeControl != null) _codeControl.PropertyChanged -= CodeControlOnPropertyChanged;
                _codeControl = value;
                if (_codeControl != null ) _codeControl.PropertyChanged += CodeControlOnPropertyChanged;
            }
        }

        private void CodeControlOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CodeControl.CustomTextSource))
            {
                CustomTextSource = CodeControl.CustomTextSource;
            }
        }

        public CustomTextSource4 CustomTextSource
        {
            get { return _customTextSource; }
            set
            {
                if (Equals(value, _customTextSource)) return;
                if (_customTextSource != null) _customTextSource.PropertyChanged -= CustomTextSourceOnPropertyChanged;
                _customTextSource = value;
                if (_customTextSource != null) _customTextSource.PropertyChanged += CustomTextSourceOnPropertyChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Length));
            }
        }

        private void CustomTextSourceOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
            if (e.PropertyName == "Runs")
            {
                OnPropertyChanged("RunCount");
            }
        }

        public int Length
        {
            get
            {
                if (CustomTextSource != null)
                {
                    var len = CustomTextSource.Dispatcher.Invoke(() => CustomTextSource.Length);
                    return len;
                }

                return -1;
            }
        }

        public int RunCount
        {
            get
            {
                if (CustomTextSource != null)
                    return CustomTextSource.Dispatcher.Invoke(() => CustomTextSource.Runs.Count);
                return -1;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}