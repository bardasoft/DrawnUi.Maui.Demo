using DrawnUi;

namespace Reversi.Views.Partials;

public partial class ScreenFun : SkiaLayout
{
    public ScreenFun(LakeViewModel vm)
    {
        this.BindingContext = vm;
        InitializeComponent();
    }

    public override void OnParentChanged(IDrawnBase newvalue, IDrawnBase oldvalue)
    {
        base.OnParentChanged(newvalue, oldvalue);

        if (Parent == null)
        {
            //looks like we got popped
            //need to dispose 
            Dispose();
        }
    }
}