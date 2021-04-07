using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace Atomex.Client.Desktop.Styles {
    public class TypeTest : TemplatedControl {
        public TypeTest() {
            if (DataContext != null) Content = DataTemplate.Build(DataContext);
        }
        
        public static readonly StyledProperty<IControl> ContentProperty = AvaloniaProperty.Register<TypeTest, IControl>(nameof(Content));

        public IControl Content {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }
        
        public static readonly StyledProperty<IDataTemplate> DataTemplateProperty = AvaloniaProperty.Register<TypeTest, IDataTemplate>(nameof(DataTemplate));

        public IDataTemplate DataTemplate {
            get => GetValue(DataTemplateProperty);
            set => SetValue(DataTemplateProperty, value);
        }
        
        protected override void OnDataContextEndUpdate() {
            base.OnDataContextEndUpdate();
            if (DataContext != null) Content = DataTemplate.Build(DataContext);
        }
    }
}