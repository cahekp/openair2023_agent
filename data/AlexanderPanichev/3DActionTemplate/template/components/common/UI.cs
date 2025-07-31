using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "c7f8afb1a259655cff57c10b04e122ee99068d75")]
public class UI : Component
{
	static UI instance;
	public static UI Get()
	{
		return instance;
	}

	public vec2 reference_resolution = new vec2(1920, 1080);

	ivec2 last_size;
	UI_Element root;
	float canvas_width;
	float canvas_height;
	Gui gui;

	public UI_Element GetRoot()
	{
		return root;
	}

	public Gui GetGui()
	{
		return gui;
	}

	public void SetCanvasSize(float reference_width, float reference_height)
	{
		reference_resolution = new vec2(reference_width, reference_height);

		float aspect_app = (float)(last_size.x) / last_size.y;
		float aspect_canvas = reference_resolution.x / reference_resolution.y;

		canvas_width = reference_resolution.x * (aspect_app / aspect_canvas);
		canvas_height = reference_resolution.y;

		// refresh widgets
		root.arrange();
	}

	public float GetCanvasWidth() { return canvas_width; }
	public float GetCanvasHeight() { return canvas_height; }

	public float GetCanvasPixelSize()
	{
		return reference_resolution.y / GetWindowHeight();
	}

	public int ConvertCanvasToScreen(float canvas_position)
	{
		return MathLib.ToInt(canvas_position / GetCanvasPixelSize());
	}

	public ivec2 ConvertCanvasToScreen(vec2 canvas_position)
	{
		float pixel_size = GetCanvasPixelSize();
		return new ivec2(
			MathLib.ToInt(canvas_position.x / pixel_size),
			MathLib.ToInt(canvas_position.y / pixel_size)
		);
	}

	public float ConvertScreenToCanvas(int screen_position)
	{
		return screen_position * GetCanvasPixelSize();
	}

	public vec2 ConvertScreenToCanvas(ivec2 screen_position)
	{
		return new vec2(screen_position) * GetCanvasPixelSize();
	}	

	public int GetWindowWidth() { return last_size.x; }
	public int GetWindowHeight() { return last_size.y; }
	public ivec2 GetWindowSize() { return last_size; }

	public float GetCanvasFontHeight(int size)
	{
		int height_px = font_size_to_height[size];
		return MathLib.ToFloat(height_px) * reference_resolution.y / GetWindowHeight();
	}

	public float GetScreenFontHeight(int size)
	{
		return MathLib.ToFloat(font_size_to_height[size]);
	}

	public int GetCanvasFontSize(float height)
	{
		float h = height * GetWindowHeight() / reference_resolution.y;
		return MathLib.ToInt(font_height_to_size.Evaluate(h));
	}

	public int GetScreenFontSize(float height_screen)
	{
		return MathLib.ToInt(font_height_to_size.Evaluate(height_screen));
	}

	[MethodInit(Order = -2)]
	void Init()
	{
		instance = this;
		last_size = WindowManager.MainWindow.ClientSize;

		gui = Gui.GetCurrent();

		calculate_font_heights();

		root = new UI_Element(0, 0, 0, 0); // zero offset from borders
		root.SetName("root");
		root.SetAnchor(0, 0, 1, 1); // expand
		root.SetPivot(0.5f, 0.5f);
		root.arrange();

		SetCanvasSize(reference_resolution.x, reference_resolution.y);
	}

	void Update()
	{
		// check resize application window
		if (!WindowManager.MainWindow)
			return; // no window - no ui
		ivec2 window_size = WindowManager.MainWindow.ClientSize;
		ivec2 window_pos = WindowManager.MainWindow.ClientPosition;

		gui.Position = window_pos;

		if (last_size != window_size)
		{
			last_size = window_size;

			// update canvas size and rearrange widgets
			SetCanvasSize(reference_resolution.x, reference_resolution.y);
			gui.Size = last_size;
		}

		// update all widgets
		root.update_hierarchy(Game.IFps);
	}

	void Shutdown()
	{

	}

	// font info
	Curve2d font_height_to_size; // size = evaluate(height)
	List<int> font_size_to_height; // font_height[getFontSize()]

	void calculate_font_heights()
	{
		font_height_to_size = new Curve2d();
		font_height_to_size.Clear();

		WidgetLabel l = new WidgetLabel("X,");
		l.Lifetime = Widget.LIFETIME.WORLD;
		font_size_to_height = new List<int>(96);
		for (int i = 1; i < 96; i++)
		{
			l.FontSize = i;
			l.Arrange();
			font_height_to_size.AddKey(new vec2((float)(l.Height), (float)(i)));
			font_size_to_height.Add(l.Height);
		}
		l.DeleteLater();
	}
}

// base, dummy ui element
public class UI_Element
{
	string name;
	float parent_widget_relative_x_offset;
	float parent_widget_relative_y_offset;
	Widget parent_widget = null;
	protected UI_Element parent;
	protected List<UI_Element> children = new List<UI_Element>();

	protected vec4 anchor;	// left top align, no stretch
	protected vec4 pos;		// default position is top left border, zero size
	protected vec2 pivot;	// top left point is zero position

	// position and size of the element (normalized)
	protected vec2 min_n = new vec2(0,0);
	protected vec2 max_n = new vec2(1,1);

	// show/hide
	bool show = true;

	public UI_Element(float x, float y, float width, float height, UI_Element parent = null)
	{
		pos.x = x;
		pos.y = y;
		pos.z = width;
		pos.w = height;

		if (parent != null)
			parent.AddChild(this);
		else if (UI.Get().GetRoot() != null)
			UI.Get().GetRoot().AddChild(this);
	}

	public virtual void Delete()
	{
		// detach from parent
		if (parent != null)
			parent.RemoveChild(this);

		// destroy children elements
		List<UI_Element> children_copy = new List<UI_Element>(children);
		for (int i = 0; i < children_copy.Count; i++)
			children_copy[i].Delete();
		children.Clear();
	}

	// name
	public void SetName(string in_name) { name = in_name; }
	public string GetName() { return name; }

	// hierarchy
	public void AddChild(UI_Element element)
	{
		if (children.FindIndex(x => x == element) != -1)
			return; // added already

		// detach from previous parent
		if (element.parent != null)
			element.parent.RemoveChild(element);

		// attach to this
		children.Add(element);
		element.parent = this;
	}

	public void RemoveChild(UI_Element element)
	{
		if (children.FindIndex(x => x == element) == -1)
			return; // removed already

		children.Remove(element);
		element.parent = null;
	}
	
	public UI_Element GetParent()
	{
		return parent; 
	}

	public int GetNumChildren()
	{
		return children.Count;
	}

	public UI_Element GetChild(int num)
	{
		return children[num];
	}

	public UI_Element FindChild(string name, bool create_if_null = false)
	{
		for (int i = 0; i < children.Count; i++)
		{
			UI_Element child = children[i];
			if (child.GetName() == name)
				return child;

			UI_Element find_in_children = child.FindChild(name);
			if (find_in_children != null)
				return find_in_children;
		}

		if (create_if_null)
		{
			UI_Element element = new UI_Element(0, 0, 0, 0);
			element.SetName(name);
			element.SetAnchorExpand();
			return element;
		}

		return null;
	}

	// z-order
	public void BringToFront()
	{
		// bring to front itself
		bring_to_front();

		// bring to front children
		for (int i = 0; i < children.Count; i++)
			children[i].bring_to_front();
	}

	// transform
	public void SetPivot(float x, float y) { pivot = new vec2(x, y); arrange(); }
	public void SetPivot(vec2 in_pivot) { pivot = in_pivot; arrange(); }
	public vec2 GetPivot() { return pivot; }
	public float GetPivotX() { return pivot.x; }
	public float GetPivotY() { return pivot.y; }

	// transform (not stretched)
	public void SetAnchor(float x_min, float y_min, float x_max, float y_max) { anchor = new vec4(x_min, y_min, x_max, y_max); arrange(); }
	public void SetAnchor(vec4 in_anchor) { anchor = in_anchor; arrange(); }
	public void SetAnchorLeftTop(bool change_pivot = true) { anchor = new vec4(0,0,0,0); if (change_pivot) pivot = new vec2(0,0); arrange(); }
	public void SetAnchorLeftMiddle(bool change_pivot = true) { anchor = new vec4(0,0.5f,0,0.5f); if (change_pivot) pivot = new vec2(0,0.5f); arrange(); }
	public void SetAnchorLeftBottom(bool change_pivot = true) { anchor = new vec4(0,1,0,1); if (change_pivot) pivot = new vec2(0,1); arrange(); }
	public void SetAnchorCenterTop(bool change_pivot = true) { anchor = new vec4(0.5f,0,0.5f,0); if (change_pivot) pivot = new vec2(0.5f,0); arrange(); }
	public void SetAnchorCenterMiddle(bool change_pivot = true) { anchor = new vec4(0.5f,0.5f,0.5f,0.5f); if (change_pivot) pivot = new vec2(0.5f,0.5f); arrange(); }
	public void SetAnchorCenterBottom(bool change_pivot = true) { anchor = new vec4(0.5f,1.0f,0.5f,1.0f); if (change_pivot) pivot = new vec2(0.5f,1.0f); arrange(); }
	public void SetAnchorRightTop(bool change_pivot = true) { anchor = new vec4(1,0,1,0); if (change_pivot) pivot = new vec2(1,0); arrange(); }
	public void SetAnchorRightMiddle(bool change_pivot = true) { anchor = new vec4(1.0f,0.5f,1.0f,0.5f); if (change_pivot) pivot = new vec2(1.0f,0.5f); arrange(); }
	public void SetAnchorRightBottom(bool change_pivot = true) { anchor = new vec4(1,1,1,1); if (change_pivot) pivot = new vec2(1,1); arrange(); }
	public vec4 GetAnchor() { return anchor; }

	public void SetPosition(vec4 position) { pos = position; arrange(); }
	public void SetPosition(vec2 position) { pos.x = position.x; pos.y = position.y; arrange(); }
	public void SetPosition(float x, float y) { pos.x = x; pos.y = y; arrange(); }
	public void SetPositionX(float x) { pos.x = x; arrange(); }
	public void SetPositionY(float y) { pos.y = y; arrange(); }
	public vec4 GetPosition() { return pos; }
	public float GetPositionX() { return pos.x; }
	public float GetPositionY() { return pos.y; }
	public void SetSize(float width, float height) { pos.z = width; pos.w = height; arrange(); }
	public void SetWidth(float width) { pos.z = width; arrange(); }
	public void SetHeight(float height) { pos.w = height; arrange(); }
	public float GetWidth() { return pos.z; }
	public float GetHeight() { return pos.w; }

	// transform (stretched)
	public void SetAnchorExpand() { anchor = new vec4(0, 0, 1, 1); arrange(); }
	public void SetAnchorStretchTop(bool change_pivot_y = true) { anchor = new vec4(0, 0, 1, 0); if (change_pivot_y) pivot.y = 0; arrange(); }
	public void SetAnchorStretchMiddle(bool change_pivot_y = true) { anchor = new vec4(0, 0.5f, 1, 0.5f); if (change_pivot_y) pivot.y = 0.5f; arrange(); }
	public void SetAnchorStretchBottom(bool change_pivot_y = true) { anchor = new vec4(0, 1, 1, 1); if (change_pivot_y) pivot.y = 1; arrange(); }
	public void SetAnchorLeftStretch(bool change_pivot_x = true) { anchor = new vec4(0, 0, 0, 1); if (change_pivot_x) pivot.x = 0; arrange(); }
	public void SetAnchorCenterStretch(bool change_pivot_x = true) { anchor = new vec4(0.5f, 0, 0.5f, 1); if (change_pivot_x) pivot.x = 0.5f; arrange(); }
	public void SetAnchorRightStretch(bool change_pivot_x = true) { anchor = new vec4(1, 0, 1, 1); if (change_pivot_x) pivot.x = 1; arrange(); }

	public void SetLeftOffset(float offset) { pos.x = offset; arrange(); }
	public void SetRightOffset(float offset) { pos.z = offset; arrange(); }
	public void SetTopOffset(float offset) { pos.y = offset; arrange(); }
	public void SetBottomOffset(float offset) { pos.w = offset; arrange(); }
	public float GetLeftOffset() { return pos.x; }
	public float GetRightOffset() { return pos.z; }
	public float GetTopOffset() { return pos.y; }
	public float GetBottomOffset() { return pos.w; }

	// show/hide
	public void SetShow(bool in_show)
	{
		// call virtual method
		set_show(in_show);

		show = in_show;

		// do the same for all children
		for (int i = 0; i < children.Count; i++)
			children[i].SetShow(in_show);
	}
	public bool IsShow() { return show; }

	// mouse input
	public bool IsHover(bool include_children = false)
	{
		// we can't hover invisible element
		if (!IsShow())
			return false;

		// check mouse is over the element
		if (is_hover())
			return true;

		// check children
		if (include_children)
		{
			for (int i = 0; i < children.Count; i++)
				if (children[i].IsHover(true))
					return true;
		}

		return false;
	}

	public bool IsDown(bool include_children = false)
	{
		return is_mouse_down() && IsHover(include_children);
	}

	// privates
	public void update_hierarchy(float ifps)
	{
		// update itself
		if (IsShow())
			update(ifps); // virtual method

		// update children
		for (int i = 0; i < children.Count; i++)
			children[i].update_hierarchy(ifps);
	}

	protected bool is_stretched_horizontally() { return anchor.x != anchor.z; }
	protected bool is_stretched_vertically() { return anchor.y != anchor.w; }
	protected int get_absolute_screen_x() { return MathLib.ToInt((min_n.x + parent_widget_relative_x_offset) * UI.Get().GetWindowWidth()); }
	protected int get_absolute_screen_y() { return MathLib.ToInt((min_n.y + parent_widget_relative_y_offset) * UI.Get().GetWindowHeight()); }
	protected int get_relative_screen_x() { return MathLib.ToInt(min_n.x * UI.Get().GetWindowWidth()); }
	protected int get_relative_screen_y() { return MathLib.ToInt(min_n.y * UI.Get().GetWindowHeight()); }
	protected int get_screen_width() { return MathLib.ToInt((max_n.x - min_n.x) * UI.Get().GetWindowWidth()); }
	protected int get_screen_height() { return MathLib.ToInt((max_n.y - min_n.y) * UI.Get().GetWindowHeight()); }
	protected bool is_mouse_down() { return !Console.Active && Input.IsMouseButtonDown(Input.MOUSE_BUTTON.LEFT); }
	protected bool is_parent_show() { return parent != null ? parent.IsShow() : true; }

	protected virtual void set_show(bool show) {}
	protected virtual bool is_hover() { return false; }
	protected virtual void update(float ifps) {}
	public virtual void arrange()
	{
		// apply parent min/max and achor
		min_n = new vec2(0,0);
		max_n = new vec2(1,1);
		if (parent != null)
		{
			float parent_width_n = parent.max_n.x - parent.min_n.x;
			float parent_height_n = parent.max_n.y - parent.min_n.y;

			min_n.x = parent.min_n.x + anchor.x * parent_width_n;
			min_n.y = parent.min_n.y + anchor.y * parent_height_n;
			max_n.x = parent.min_n.x + anchor.z * parent_width_n;
			max_n.y = parent.min_n.y + anchor.w * parent_height_n;
		}
		else
		{
			min_n.x = anchor.x;
			min_n.y = anchor.y;
			max_n.x = anchor.z;
			max_n.y = anchor.w;
		}

		// get normalized position (offsets) of the element
		vec4 pos_n = new vec4(
			pos.x / UI.Get().GetCanvasWidth(),
			pos.y / UI.Get().GetCanvasHeight(),
			pos.z / UI.Get().GetCanvasWidth(),
			pos.w / UI.Get().GetCanvasHeight()
		);

		parent_widget_relative_x_offset = 0;
		parent_widget_relative_y_offset = 0;
		if (parent_widget && parent_widget != UI.Get().GetGui().VBox)
		{
			ivec2 window_size = UI.Get().GetWindowSize();
			float relative_pixel_w = 1.0f / window_size.x;
			float relative_pixel_h = 1.0f / window_size.y;
			parent_widget_relative_x_offset = -parent_widget.ScreenPositionX * relative_pixel_w;
			parent_widget_relative_y_offset = -parent_widget.ScreenPositionY * relative_pixel_h;
		}

		// apply pos
		if (anchor.x != anchor.z) // stretched
		{
			min_n.x += pos_n.x; // offset
			max_n.x -= pos_n.z; // offset
		}
		else
		{
			min_n.x += pos_n.x - pos_n.z * pivot.x; // pos x
			max_n.x = min_n.x + pos_n.z; // width
		}

		if (anchor.y != anchor.w) // stretched
		{
			min_n.y += pos_n.y; // offset
			max_n.y -= pos_n.w; // offset
		}
		else
		{
			min_n.y += pos_n.y - pos_n.w * pivot.y; // pos y
			max_n.y = min_n.y + pos_n.w; // height
		}

		// update children
		foreach (var child in children)
			child.arrange();
	}

	protected virtual void bring_to_front() {}

	protected ivec2 get_mouse_position()
	{
		if (!WindowManager.MainWindow)
			return new ivec2(0, 0);
		return Input.MousePosition - WindowManager.MainWindow.ClientPosition;
	}
};

public class UI_Sprite : UI_Element
{
	WidgetSprite sprite_w;
	int s_x, s_y; // in screen pixels
	int s_w, s_h;
	bool fixed_ratio = false;
	float angle = 0;
	vec3 sprite_size;

	public UI_Sprite(float x, float y, float width, float height, string name = "white.texture", UI_Element parent = null) : base(x, y, width, height, parent)
	{
		sprite_w = new WidgetSprite(UI.Get().GetGui(), name);
		sprite_w.Lifetime = Widget.LIFETIME.WORLD;
		sprite_w.Arrange();
		sprite_size = new vec3(MathLib.ToFloat(sprite_w.Width), MathLib.ToFloat(sprite_w.Height), 0);
		UI.Get().GetGui().AddChild(sprite_w, Gui.ALIGN_OVERLAP);

		arrange();
	}

	public override void Delete()
	{
		sprite_w.DeleteLater();
		base.Delete();
	}

	public void SetTexture(string texture_path)
	{
		sprite_w.Width = 0;
		sprite_w.Height = 0;
		sprite_w.Texture = texture_path;
		sprite_w.Arrange();
		sprite_size = new vec3(MathLib.ToFloat(sprite_w.Width), MathLib.ToFloat(sprite_w.Height), 0);
		arrange();	
	}
	public string GetTexture() { return sprite_w.Texture; }

	public void SetColor(vec4 color) { sprite_w.Color = color; }
	public void SetColor(float r, float g, float b, float a) { sprite_w.Color = new vec4(r, g, b, a); }
	public vec4 GetColor() { return sprite_w.Color; }

	public void SetFixedRatio(bool in_fixed_ratio) { fixed_ratio = in_fixed_ratio; }

	public void SetRotation(float angle_deg) { angle = angle_deg; arrange(); }
	public float GetRotation() { return angle; }

	public WidgetSprite GetWidget() { return sprite_w; }

	protected override void set_show(bool show) { sprite_w.Hidden = !show; }
	protected override bool is_hover()
	{
		ivec2 pos = get_mouse_position();
		if (pos.x > sprite_w.ScreenPositionX &&
			pos.y > sprite_w.ScreenPositionY &&
			pos.x - sprite_w.ScreenPositionX < sprite_w.Width &&
			pos.y - sprite_w.ScreenPositionY < sprite_w.Height)
			return true;
		return false;
	}
	protected override void update(float ifps) { base.update(ifps); }
	public override void arrange()
	{
		base.arrange();

		s_x = get_absolute_screen_x();
		s_y = get_absolute_screen_y();
		s_w = MathLib.Max(1, get_screen_width());
		s_h = MathLib.Max(1, get_screen_height());

		if (fixed_ratio)
		{
			float sprite_ratio = sprite_size.x / sprite_size.y;
			float area_ratio = MathLib.ToFloat(s_w) / s_h;
			if (area_ratio > sprite_ratio)
			{
				float new_w = s_h * sprite_ratio;
				s_x += MathLib.ToInt((s_w - new_w) * pivot.x);
				s_w = MathLib.ToInt(new_w);
			}
			else
			{
				float new_h = s_w / sprite_ratio;
				s_y += MathLib.ToInt((s_h - new_h) * pivot.y);
				s_h = MathLib.ToInt(new_h);
			}
		}

		if (angle == 0)
		{
			sprite_w.SetPosition(s_x, s_y);
			sprite_w.Width = s_w;
			sprite_w.Height = s_h;
			sprite_w.Transform.SetIdentity();
		}
		else
		{
			sprite_w.SetPosition(
				s_x + MathLib.ToInt((s_w - sprite_size.x) * pivot.x),
				s_y + MathLib.ToInt((s_h - sprite_size.y) * pivot.y));
			sprite_w.Width = 0;
			sprite_w.Height = 0;

			vec3 t = new vec3(sprite_size.x * pivot.x, sprite_size.y * pivot.y, 0);
			sprite_w.Transform =
				MathLib.Translate(t) *
				MathLib.RotateZ(angle) *
				MathLib.Scale(s_w / sprite_size.x, s_h / sprite_size.y, 1) *
				MathLib.Translate(-t);
		}
		sprite_w.Arrange();
	}
	
	protected override void bring_to_front()
	{
		Widget parent = sprite_w.Parent;
		parent.RemoveChild(sprite_w);
		parent.AddChild(sprite_w, Gui.ALIGN_OVERLAP);
	}
}

public class UI_Label : UI_Element
{
	public enum HORIZONTAL_ALIGN { LEFT, CENTER, RIGHT }
	public enum VERTICAL_ALIGN { TOP, MIDDLE, BOTTOM }

	WidgetLabel label;
	int lx, ly, lw, lh;
	float font_height;
	HORIZONTAL_ALIGN h_align = HORIZONTAL_ALIGN.LEFT;
	VERTICAL_ALIGN v_align = VERTICAL_ALIGN.TOP;
	vec4 font_color = new vec4(1,1,1,1);

	public UI_Label(float x, float y, float width, float height, string text = "", UI_Element parent = null) : base(x, y, width, height, parent)
	{
		label = new WidgetLabel(UI.Get().GetGui());
		label.Lifetime = Widget.LIFETIME.WORLD;
		UI.Get().GetGui().AddChild(label, Gui.ALIGN_OVERLAP);

		label.FontOutline = 1;
		SetFontHeight(height);
		SetText(text);
		SetFontColor(new vec4(1,1,1,1));

		arrange();
	}

	public UI_Label(float x, float y, float font_size, string text = "", UI_Element parent = null) : base(x, y, 0, 0, parent)
	{
		label = new WidgetLabel(UI.Get().GetGui());
		label.Lifetime = Widget.LIFETIME.WORLD;
		UI.Get().GetGui().AddChild(label, Gui.ALIGN_OVERLAP);

		label.FontOutline = 1;
		SetFontHeight(font_size);
		SetText(text);
		SetFontColor(new vec4(1,1,1,1));

		arrange();
	}
	
	public override void Delete()
	{
		label.DeleteLater();
		base.Delete();
	}

	public void SetFont(string font) { label.SetFont(font); }

	public void SetText(string text) { label.Text = text; arrange(); }
	public string GetText() { return label.Text; }

	public vec2 GetTextSize()
	{
		label.Arrange();
		return new vec2(
			label.Width * UI.Get().GetCanvasPixelSize(),
			label.Height * UI.Get().GetCanvasPixelSize());
	}
	
	public void SetHorizontalAlign(HORIZONTAL_ALIGN align)
	{
		h_align = align;
		switch (align)
		{
			case HORIZONTAL_ALIGN.LEFT: label.TextAlign = Gui.ALIGN_LEFT; break;
			case HORIZONTAL_ALIGN.CENTER: label.TextAlign = Gui.ALIGN_CENTER; break;
			case HORIZONTAL_ALIGN.RIGHT: label.TextAlign = Gui.ALIGN_RIGHT; break;
		}
		arrange();
	}

	public HORIZONTAL_ALIGN GetHorizontalAlign() { return h_align; }

	public void SetVerticalAlign(VERTICAL_ALIGN align)
	{
		v_align = align;
		arrange();
	}

	public VERTICAL_ALIGN GetVerticalAlign() { return v_align; }

	public void SetTextAlign(HORIZONTAL_ALIGN h, VERTICAL_ALIGN v = VERTICAL_ALIGN.TOP)
	{
		h_align = h;
		v_align = v;
		switch (h_align)
		{
			case HORIZONTAL_ALIGN.LEFT: label.TextAlign = Gui.ALIGN_LEFT; break;
			case HORIZONTAL_ALIGN.CENTER: label.TextAlign = Gui.ALIGN_CENTER; break;
			case HORIZONTAL_ALIGN.RIGHT: label.TextAlign = Gui.ALIGN_RIGHT; break;
		}
		arrange();
	}

	public void SetFontHeight(float height) { font_height = height; arrange(); }
	public float GetFontHeight() { return font_height; }

	public void SetWordWrap(bool wrap)
	{
		label.FontWrap = wrap ? 1 : 0;
		label.Width = wrap ? lw : 0;
		arrange();
	}
	public bool IsWordWrap() { return label.FontWrap > 0; }

	public void SetFontRich(bool rich) { label.FontRich = rich ? 1 : 0; }
	public bool IsFontRich() { return label.FontRich > 0; }

	public void SetFontVSpacig(int spacing) { label.FontVSpacing = spacing; }
	public int GetFontVSpacig() { return label.FontVSpacing; }

	public void setFontOutline(bool outline) { label.FontOutline = outline ? 1 : 0; }
	public bool isFontOutline() { return label.FontOutline > 0; }

	public void SetFontColor(vec4 color)
	{
		font_color = color;
		label.FontColor = color;
	}

	public vec4 GetFontColor() { return font_color; }

	public WidgetLabel GetWidget() { return label; }

	protected override void set_show(bool show)
	{
		label.FontColor = font_color; // fixed "gray color" hanging
		label.Hidden = !show;
		apply_position_and_size();		
	}

	protected override bool is_hover()
	{
		ivec2 pos = get_mouse_position();
		if (pos.x > label.ScreenPositionX &&
			pos.y > label.ScreenPositionY &&
			pos.x - label.ScreenPositionX < label.Width &&
			pos.y - label.ScreenPositionY < label.Height)
			return true;
		return false;
	}

	protected override void update(float ifps) {}
	public override void arrange()
	{
		base.arrange();

		lx = get_absolute_screen_x();
		ly = get_absolute_screen_y();
		lw = get_screen_width();
		lh = get_screen_height();

		apply_position_and_size();
		apply_position_and_size(); // fix engine bug 		
	}

	protected void apply_position_and_size()
	{
		if (label && label.Gui == null)
			return;
	
		// calculate and change font size, word wrap
		label.FontSize = UI.Get().GetCanvasFontSize(font_height);
		label.Width = IsWordWrap() ? lw : 0;
		label.Arrange();

		// calculate alignments
		int x = lx;
		int y = ly;

		switch (h_align)
		{
			case HORIZONTAL_ALIGN.CENTER: x = lx + lw / 2 - label.Width / 2; break;
			case HORIZONTAL_ALIGN.RIGHT: x = lx + lw - label.Width; break;
		}
		switch (v_align)
		{
			case VERTICAL_ALIGN.MIDDLE: y = ly + lh / 2 - label.Height / 2; break;
			case VERTICAL_ALIGN.BOTTOM: y = ly + lh - label.Height; break;
		}

		// apply position
		label.SetPosition(x, y);
	}

	protected override void bring_to_front()
	{
		Widget parent = label.Parent;
		parent.RemoveChild(label);
		parent.AddChild(label, Gui.ALIGN_OVERLAP);
	}
}

public class UI_Button : UI_Element
{
	WidgetSprite button;
	WidgetLabel label;
	int bx, by, bw, bh;

	public enum ALIGN { LEFT, CENTER, RIGHT };
	ALIGN text_align = ALIGN.LEFT;
	string text_original;
	float font_size_percent = 1.0f;
	float font_size_offset = 0.0f;

	vec4 col_hover = new vec4(0.145f, 0.73f, 0.9f, 1);
	vec4 col_default = new vec4(0.29f, 0.54f, 0.61f, 1);
	vec4 col_label_hover = new vec4(0, 0, 0, 1);
	vec4 col_label_default = new vec4(1, 1, 1, 1);

	public UI_Button(float x, float y, float width, float height,
		string text = "", UI_Element parent = null,
		float font_size_percent = 1.0f) : base(x, y, width, height, parent)
	{
		button = new WidgetSprite(UI.Get().GetGui(), "white.texture");
		button.Lifetime = Widget.LIFETIME.WORLD;
		UI.Get().GetGui().AddChild(button, Gui.ALIGN_OVERLAP);
		button.Color = col_default;

		label = new WidgetLabel();
		label.Lifetime = Widget.LIFETIME.WORLD;
		UI.Get().GetGui().AddChild(label, Gui.ALIGN_OVERLAP);
		label.FontColor = col_label_default;
		label.FontOutline = 1;
		label.FontRich = 1;
		label.Text = text;
		text_original = text;
		this.font_size_percent = font_size_percent;

		arrange();
	}
	
	public override void Delete()
	{
		button.DeleteLater();
		label.DeleteLater();
		base.Delete();
	}

	public void SetColorHover(vec4 color) { col_hover = color; }
	public void SetColorDefault(vec4 color) { col_default = color; }

	public void SetLabelColorHover(vec4 color) { col_label_hover = color; }
	public void SetLabelColorDefault(vec4 color) { col_label_default = color; }

	public void SetText(string text)
	{ 
		text_original = text;
		SetTextAlign(text_align);
		label.Arrange();
	}
	public string GetText() { return text_original; }

	public void SetTextAlign(ALIGN align)
	{
		label.FontRich = 1;
		text_align = align;
		switch (align)
		{
		case ALIGN.LEFT: label.Text = "<p align=left>" + text_original + "</p>"; break;
		case ALIGN.CENTER: label.Text = "<p align=center>" + text_original + "</p>"; break;
		case ALIGN.RIGHT: label.Text = "<p align=right>" + text_original + "</p>"; break;
		}		
	}
	public ALIGN GetTextAlign() { return text_align; }

	public void SetFontSizePercent(float percent) { font_size_percent = percent; apply_position_and_size(); }
	public float GetFontSizePercent() { return font_size_percent; }

	public void SetFontSizeOffset(float offset) { font_size_offset = offset; apply_position_and_size(); }
	public float GetFontSizeOffset() { return font_size_offset; }

	// unigine widgets
	public WidgetSprite GetWidgetButton() { return button; }
	public WidgetLabel GetWidgetLabel() { return label; }

	protected override void set_show(bool show)
	{
		button.Hidden = !show;
		label.Hidden = !show;
	}

	protected override bool is_hover()
	{
		ivec2 pos = get_mouse_position();
		if (pos.x > button.ScreenPositionX &&
			pos.y > button.ScreenPositionY &&
			pos.x - button.ScreenPositionX < button.Width &&
			pos.y - button.ScreenPositionY < button.Height)
			return true;
		return false;
	}

	protected override void update(float ifps)
	{
		base.update(ifps);

		bool hovering = is_hover() && IsShow();
		button.Color = MathLib.Lerp(button.Color, hovering ? col_hover : col_default, MathLib.Saturate(15.0f * ifps));
		label.FontColor = MathLib.Lerp(label.FontColor, hovering ? col_label_hover : col_label_default, MathLib.Saturate(15.0f * ifps));
	}

	public override void arrange()
	{
		base.arrange();

		bx = get_absolute_screen_x();
		by = get_absolute_screen_y();
		bw = get_screen_width();
		bh = get_screen_height();

		apply_position_and_size();
		apply_position_and_size(); // i don't know why, but label needs to be arranged twice
	}

	protected void apply_position_and_size()
	{
		button.SetPosition(bx, by);
		button.Width = bw;
		button.Height = bh;
		button.Arrange();

		float font_height = font_size_percent * MathLib.ToFloat(bh) * 0.8f;
		label.FontSize = UI.Get().GetScreenFontSize(font_height);
		label.Arrange();
		int ly = MathLib.ToInt((bh - font_height) / 2 - UI.Get().GetScreenFontSize(font_size_offset));
		switch (text_align)
		{
		case ALIGN.LEFT: label.SetPosition(bx + ly, by + ly); break;
		case ALIGN.CENTER: label.SetPosition(bx, by + ly); break;
		case ALIGN.RIGHT: label.SetPosition(bx - ly, by + ly); break;
		}
		label.Width = bw;
		label.Height = bh;
	}

	protected override void bring_to_front()
	{
		Widget parent = button.Parent;
		parent.RemoveChild(button);
		parent.AddChild(button, Gui.ALIGN_OVERLAP);

		parent = label.Parent;
		parent.RemoveChild(label);
		parent.AddChild(label, Gui.ALIGN_OVERLAP);
	}
}