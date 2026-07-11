using Android.App;
using Android.InputMethodServices;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using System;

namespace sammadkeyboard
{
    [Service(Name = "com.sammad.keyboard.SammadIME", Permission = "android.permission.BIND_INPUT_METHOD", Label = "كيبورد صماد | Sammad")]
    [IntentFilter(new[] { "android.view.InputMethod" })]
    [MetaData("android.view.im", Resource = "@xml/method")]
    public class SammadIME : InputMethodService
    {
        private LinearLayout mainLayout;
        private LinearLayout toolbarPanel;
        private LinearLayout keyboardPanel;

        // حالات لوحات العرض (التنقل بين الشاشات)
        private bool isMenuMode = false;
        private bool isTextEditMode = false;

        // درجات الألوان مأخوذة بدقة متناهية من صورك المرجعية
        private readonly Color bgPink = Color.ParseColor("#FFEBEB");       // الوردي الفاتح جداً للخلفية الكلية
        private readonly Color btnDarkPink = Color.ParseColor("#FFCDCD");   // الوردي الداكن لأزرار التحكم الجانبية
        private readonly Color btnWhite = Color.ParseColor("#FFFFFF");      // الأبيض الناصع لأزرار الأسهم والحروف
        private readonly Color txtDark = Color.ParseColor("#3C3C3C");       // لون النصوص والرموز الداكن للفصل

        public override View OnCreateInputView()
        {
            // 1. الحاوية الرئيسية الطولية للكيبورد
            mainLayout = new LinearLayout(this) { Orientation = Orientation.Vertical };
            mainLayout.SetBackgroundColor(bgPink);
            mainLayout.SetPadding(10, 5, 10, 10);

            // 2. شريط الأدوات العلوي الثابت
            toolbarPanel = new LinearLayout(this) { Orientation = Orientation.Horizontal };
            toolbarPanel.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 110);
            toolbarPanel.SetGravity(GravityFlags.CenterVertical);

            // 3. حاوية الأزرار السفلية الديناميكية المتغيرة
            keyboardPanel = new LinearLayout(this) { Orientation = Orientation.Vertical };
            keyboardPanel.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

            mainLayout.AddView(toolbarPanel);
            mainLayout.AddView(keyboardPanel);

            UpdateKeyboardDisplay();

            return mainLayout;
        }

        private void UpdateKeyboardDisplay()
        {
            BuildToolbar();
            keyboardPanel.RemoveAllViews();

            if (isTextEditMode)
                BuildTextEditLayout();
            else if (isMenuMode)
                BuildMenuLayout();
            else
                BuildArabicLettersLayout(); // الواجهة الافتراضية عند فتح الكيبورد
        }

        // شريط الأدوات العلوي (تطابق تماماً الصورة 1000080477)
        private void BuildToolbar()
        {
            toolbarPanel.RemoveAllViews();

            // أيقونة الميكروفون أقصى اليسار
            TextView micIcon = new TextView(this) { Text = "🎙️", TextSize = 18, Gravity = GravityFlags.Center };
            LinearLayout.LayoutParams micParams = new LinearLayout.LayoutParams(90, 90);
            micParams.SetMargins(10, 0, 10, 0);
            micIcon.LayoutParameters = micParams;

            // اقتراحات الكلمات في المنتصف بالفواصل الطولية المرجعية
            TextView txtSuggestions = new TextView(this) 
            { 
                Text = "عجلات   |   عجلة   |   عح", 
                TextSize = 15, 
                Gravity = GravityFlags.Center,
                LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, 1f)
            };
            txtSuggestions.SetTextColor(txtDark);

            // زر المربعات الأربعة (أقصى اليمين) لفتح وإغلاق قائمة الميزات
            TextView menuIcon = new TextView(this) { Text = "░", TextSize = 20, Gravity = GravityFlags.Center };
            menuIcon.LayoutParameters = new LinearLayout.LayoutParams(90, 90);
            menuIcon.Click += (s, e) => {
                isMenuMode = !isMenuMode;
                isTextEditMode = false;
                UpdateKeyboardDisplay();
            };

            toolbarPanel.AddView(micIcon);
            toolbarPanel.AddView(txtSuggestions);
            toolbarPanel.AddView(menuIcon);
        }

        // واجهة الميزات والأيقونات المربعة (تطابق تماماً الصورة 1000080478)
        private void BuildMenuLayout()
        {
            // النص الإرشادي العلوي المكتوب بالصورة
            TextView hintTxt = new TextView(this) 
            { 
                Text = "يمكنك الضغط مع الاستمرار على نقاط الوصول وسحبها لإعادة ترتيبها.",
                TextSize = 12, Gravity = GravityFlags.Right, Padding = new Padding(0, 10, 20, 10)
            };
            hintTxt.SetTextColor(txtDark);
            keyboardPanel.AddView(hintTxt);

            // الصف الأول للميزات
            LinearLayout row1 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
            row1.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

            View btnTextEdit = CreateFeatureItem("📝", "تعديل النصوص");
            btnTextEdit.Click += (s, e) => {
                isTextEditMode = true;
                isMenuMode = false;
                UpdateKeyboardDisplay();
            };

            row1.AddView(CreateFeatureItem("👤", "بيد واحدة"));
            row1.AddView(btnTextEdit);
            row1.AddView(CreateFeatureItem("📋", "الحافظة"));
            row1.AddView(CreateFeatureItem("⌨️", "عائم"));
            keyboardPanel.AddView(row1);

            // الصف الثاني للميزات
            LinearLayout row2 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
            row2.AddView(CreateFeatureItem("⚙️", "الإعدادات"));
            row2.AddView(CreateFeatureItem("🖼️", "ملف GIF"));
            row2.AddView(CreateFeatureItem("🔗", "مشاركة"));
            row2.AddView(CreateFeatureItem("🌐", "ترجمة"));
            keyboardPanel.AddView(row2);
        }

        // واجهة تعديل النصوص الاحترافية بالأسهم (تطابق تماماً الصورة 1000080471)
        private void BuildTextEditLayout()
        {
            // هيدر اللوحة: يحتوي على سهم العودة الأبيض وعنوان "تعديل النصوص" بالمنتصف
            RelativeLayout header = new RelativeLayout(this);
            header.LayoutParameters = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 100);

            TextView title = new TextView(this) { Text = "تعديل النصوص", TextSize = 16, Typeface = Typeface.DefaultBold };
            title.SetTextColor(txtDark);
            RelativeLayout.LayoutParams titleParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            titleParams.AddRule(LayoutRules.CenterInParent);
            title.LayoutParameters = titleParams;

            Button backBtn = new Button(this) { Text = "➔", BackgroundColor = btnWhite };
            backBtn.SetTextColor(Color.Black);
            RelativeLayout.LayoutParams backParams = new RelativeLayout.LayoutParams(80, 80);
            backParams.AddRule(LayoutRules.AlignParentLeft);
            backParams.AddRule(LayoutRules.CenterVertical);
            backBtn.LayoutParameters = backParams;
            backBtn.Click += (s, e) => { isTextEditMode = false; isMenuMode = true; UpdateKeyboardDisplay(); };

            header.AddView(title);
            header.AddView(backBtn);
            keyboardPanel.AddView(header);

            // تقسيم الشاشة السفلي هندسياً: الأسهم يساراً وأزرار التحكم يميناً
            LinearLayout bodyLayout = new LinearLayout(this) { Orientation = Orientation.Horizontal };
            
            // 1. كتلة الأسهم اليسرى الفسيحة (بيضاء بالكامل)
            LinearLayout leftArrowsBlock = new LinearLayout(this) { Orientation = Orientation.Vertical };
            leftArrowsBlock.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 3f);

            LinearLayout arrowsRow1 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
            Button btnLeftArrow = CreateEditButton("<", 110, 240, btnWhite, txtDark); // سهم يسار ممتد طولياً
            
            LinearLayout centerArrowStack = new LinearLayout(this) { Orientation = Orientation.Vertical };
            centerArrowStack.AddView(CreateEditButton("＾", 140, 115, btnWhite, txtDark));
            centerArrowStack.AddView(CreateEditButton("تحديد", 140, 115, btnWhite, txtDark));
            centerArrowStack.AddView(CreateEditButton("ｖ", 140, 115, btnWhite, txtDark));

            Button btnRightArrow = CreateEditButton(">", 110, 240, btnWhite, txtDark); // سهم يمين ممتد طولياً

            arrowsRow1.AddView(btnLeftArrow);
            arrowsRow1.AddView(centerArrowStack);
            arrowsRow1.AddView(btnRightArrow);
            leftArrowsBlock.AddView(arrowsRow1);

            // أزرار الانتقال السفلي (البداية والنهاية للسطر)
            LinearLayout arrowsRow2 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
            arrowsRow2.AddView(CreateEditButton("|＜", 180, 110, btnWhite, txtDark));
            arrowsRow2.AddView(CreateEditButton("＞|", 180, 110, btnWhite, txtDark));
            leftArrowsBlock.AddView(arrowsRow2);

            // 2. كتلة التحكم الجانبية اليمنى (وردي داكن للتحكم والعمليات)
            LinearLayout rightControlBlock = new LinearLayout(this) { Orientation = Orientation.Vertical };
            rightControlBlock.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1.2f);
            
            rightControlBlock.AddView(CreateEditButton("تحديد الكل", 160, 110, btnDarkPink, txtDark));
            rightControlBlock.AddView(CreateEditButton("نسخ", 160, 110, btnDarkPink, txtDark));
            rightControlBlock.AddView(CreateEditButton("لصق", 160, 110, btnDarkPink, txtDark));
            rightControlBlock.AddView(CreateEditButton("⌫", 160, 110, btnDarkPink, txtDark));

            bodyLayout.AddView(leftArrowsBlock);
            bodyLayout.AddView(rightControlBlock);
            keyboardPanel.AddView(bodyLayout);
        }

        // واجهة الحروف القياسية الأساسية
        private void BuildArabicLettersLayout()
        {
            TextView placeholder = new TextView(this) { Text = "هنا تظهر لوحة الحروف العربية القياسية", Gravity = GravityFlags.Center, Padding = new Padding(0, 50, 0, 50) };
            placeholder.SetTextColor(txtDark);
            keyboardPanel.AddView(placeholder);
        }

        // دالة مساعدة لبناء مربعات شبكة الميزات
        private LinearLayout CreateFeatureItem(string icon, string label)
        {
            LinearLayout item = new LinearLayout(this) { Orientation = Orientation.Vertical, Gravity = GravityFlags.Center };
            LinearLayout.LayoutParams p = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);
            p.SetMargins(5, 5, 5, 5);
            item.LayoutParameters = p;
            item.SetBackgroundColor(btnWhite);
            item.SetPadding(5, 15, 5, 15);

            TextView txtIcon = new TextView(this) { Text = icon, TextSize = 18, Gravity = GravityFlags.Center };
            TextView txtLabel = new TextView(this) { Text = label, TextSize = 10, Gravity = GravityFlags.Center };
            txtLabel.SetTextColor(txtDark);

            item.AddView(txtIcon);
            item.AddView(txtLabel);
            return item;
        }

        // دالة مساعدة لضبط قياسات وهندسة أزرار الأسهم الدقيقة
        private Button CreateEditButton(string text, int w, int h, Color bg, Color fg)
        {
            Button btn = new Button(this) { Text = text, BackgroundColor = bg };
            btn.SetTextColor(fg);
            btn.TextSize = 13;
            LinearLayout.LayoutParams p = new LinearLayout.LayoutParams(w, h);
            p.SetMargins(4, 4, 4, 4);
            btn.LayoutParameters = p;
            return btn;
        }
    }
}
