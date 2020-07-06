using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.TextFormatting;
using JetBrains.Annotations;

namespace RoslynCodeControls
{
    public class CustomTextSource4Proxy : INotifyPropertyChanged
    {
        private RoslynCodeControl _codeControl;
        private CustomTextSource4 _customTextSource;
        private int _runCount;
        private IEnumerable<TextRun> _runs;

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
                Runs = _customTextSource.Runs;
            }
        }

        private void CustomTextSourceOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Runs")
            {
                Runs = CustomTextSource.Runs;
                return;
            }
            OnPropertyChanged(e.PropertyName);
            
        }

        private void RunsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RunCount = Runs.Count();
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

        public IEnumerable<TextRun> Runs
        {
            get { return _runs; }
            set
            {
                if (Equals(value, _runs)) return;
                if (_runs is INotifyCollectionChanged runs)
                {
                    runs.CollectionChanged -= RunsOnCollectionChanged;
                }

                _runs = value;
                if (_runs is INotifyCollectionChanged runs2)
                {
                    runs2.CollectionChanged += RunsOnCollectionChanged;
                }

                RunCount = _runs?.Count() ?? 0;

                OnPropertyChanged();
            }
        }

        public int RunCount
        {
            get { return _runCount; }
            set
            {
                if (value == _runCount) return;
                _runCount = value;
                OnPropertyChanged();
            }
        }
        // public int RunCount
        // {
        // get
        // {
        // if (CustomTextSource != null)
        // return CustomTextSource.Dispatcher.Invoke(() => CustomTextSource.Runs.Count);
        // return -1;
        // }
        // }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}