using System;
namespace Reversi
{
    public class AboutDialog : System.Windows.Forms.Form
    {
        private System.Windows.Forms.PictureBox iconPictureBox;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.Label copyrightLabel;
        private System.ComponentModel.Container components;
        protected virtual void Dispose(bool disposing)
        {
            if (!(!disposing)) {
                if (!(!(IL__ldfld(components, @this)))) {
                    IL__callvirt(Dispose(), (IL__ldfld(components, @this)));
                    goto BasicBlock_4;
                }
                else {
                    goto BasicBlock_4;
                }
            }
            BasicBlock_4:
            @this.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            System.Resources.ResourceManager V_0 = (IL__newobj(.ctor(), Type.GetTypeFromHandle((IL__ldtoken(Reversi.AboutDialog)))));
            IL__stfld(iconPictureBox, @this, (IL__newobj(.ctor())));
            IL__stfld(titleLabel, @this, (IL__newobj(.ctor())));
            IL__stfld(versionLabel, @this, (IL__newobj(.ctor())));
            IL__stfld(okButton, @this, (IL__newobj(.ctor())));
            IL__stfld(copyrightLabel, @this, (IL__newobj(.ctor())));
            @this.SuspendLayout();
            IL__callvirt(set_Location(), (IL__ldfld(iconPictureBox, @this)), (IL__newobj(.ctor(), 77, 56)));
            IL__callvirt(set_Name(), (IL__ldfld(iconPictureBox, @this)), "iconPictureBox");
            IL__callvirt(set_Size(), (IL__ldfld(iconPictureBox, @this)), (IL__newobj(.ctor(), 96, 96)));
            IL__callvirt(set_TabIndex(), (IL__ldfld(iconPictureBox, @this)), 0);
            IL__callvirt(set_TabStop(), (IL__ldfld(iconPictureBox, @this)), 0);
            IL__callvirt(set_AutoSize(), (IL__ldfld(titleLabel, @this)), 1);
            IL__callvirt(set_Font(), (IL__ldfld(titleLabel, @this)), (IL__newobj(.ctor(), "Microsoft Sans Serif", 8.25f, 1, 3, 0)));
            IL__callvirt(set_Location(), (IL__ldfld(titleLabel, @this)), (IL__newobj(.ctor(), 103, 16)));
            IL__callvirt(set_Name(), (IL__ldfld(titleLabel, @this)), "titleLabel");
            IL__callvirt(set_Size(), (IL__ldfld(titleLabel, @this)), (IL__newobj(.ctor(), 44, 16)));
            IL__callvirt(set_TabIndex(), (IL__ldfld(titleLabel, @this)), 0);
            IL__callvirt(set_Text(), (IL__ldfld(titleLabel, @this)), "Reversi");
            IL__callvirt(set_TextAlign(), (IL__ldfld(titleLabel, @this)), 32);
            IL__callvirt(set_AutoSize(), (IL__ldfld(versionLabel, @this)), 1);
            IL__callvirt(set_Location(), (IL__ldfld(versionLabel, @this)), (IL__newobj(.ctor(), 95, 32)));
            IL__callvirt(set_Name(), (IL__ldfld(versionLabel, @this)), "versionLabel");
            IL__callvirt(set_Size(), (IL__ldfld(versionLabel, @this)), (IL__newobj(.ctor(), 61, 16)));
            IL__callvirt(set_TabIndex(), (IL__ldfld(versionLabel, @this)), 1);
            IL__callvirt(set_Text(), (IL__ldfld(versionLabel, @this)), "Version 2.0");
            IL__callvirt(set_TextAlign(), (IL__ldfld(versionLabel, @this)), 32);
            IL__callvirt(set_DialogResult(), (IL__ldfld(okButton, @this)), 1);
            IL__callvirt(set_Location(), (IL__ldfld(okButton, @this)), (IL__newobj(.ctor(), 88, 192)));
            IL__callvirt(set_Name(), (IL__ldfld(okButton, @this)), "okButton");
            IL__callvirt(set_TabIndex(), (IL__ldfld(okButton, @this)), 3);
            IL__callvirt(set_Text(), (IL__ldfld(okButton, @this)), "OK");
            IL__callvirt(set_AutoSize(), (IL__ldfld(copyrightLabel, @this)), 1);
            IL__callvirt(set_Location(), (IL__ldfld(copyrightLabel, @this)), (IL__newobj(.ctor(), 36, 160)));
            IL__callvirt(set_Name(), (IL__ldfld(copyrightLabel, @this)), "copyrightLabel");
            IL__callvirt(set_Size(), (IL__ldfld(copyrightLabel, @this)), (IL__newobj(.ctor(), 178, 16)));
            IL__callvirt(set_TabIndex(), (IL__ldfld(copyrightLabel, @this)), 2);
            IL__callvirt(set_Text(), (IL__ldfld(copyrightLabel, @this)), "Copyright 2003-2005 by Mike Hall.");
            IL__callvirt(set_TextAlign(), (IL__ldfld(copyrightLabel, @this)), 32);
            @this.set_AcceptButton((IL__ldfld(okButton, @this)));
            IL__callvirt(set_AutoScaleBaseSize(), @this, (IL__newobj(.ctor(), 5, 13)));
            @this.set_CancelButton((IL__ldfld(okButton, @this)));
            @this.set_ClientSize((IL__newobj(.ctor(), 250, 224)));
            @this.set_ControlBox(0);
            IL__callvirt(Add(), @this.get_Controls(), (IL__ldfld(copyrightLabel, @this)));
            IL__callvirt(Add(), @this.get_Controls(), (IL__ldfld(versionLabel, @this)));
            IL__callvirt(Add(), @this.get_Controls(), (IL__ldfld(titleLabel, @this)));
            IL__callvirt(Add(), @this.get_Controls(), (IL__ldfld(okButton, @this)));
            IL__callvirt(Add(), @this.get_Controls(), (IL__ldfld(iconPictureBox, @this)));
            @this.set_FormBorderStyle(3);
            @this.set_Icon((IL__castclass(System.Drawing.Icon, (IL__callvirt(GetObject(), V_0, "$this.Icon")))));
            @this.set_MaximizeBox(0);
            @this.set_MinimizeBox(0);
            @this.set_Name("AboutDialog");
            @this.set_ShowInTaskbar(0);
            @this.set_StartPosition(4);
            IL__callvirt(set_Text(), @this, "About Reversi");
            @this.ResumeLayout(0);
        }
    }
}
