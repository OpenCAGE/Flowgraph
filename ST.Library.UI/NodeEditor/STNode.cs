using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using CATHODE.Scripting;
using CATHODE.Scripting.Internal;
using System.Drawing.Drawing2D;
/*
MIT License

Copyright (c) 2021 DebugST@crystal_lz

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */
/*
 * create: 2021-12-08
 * modify: 2021-03-02
 * Author: Crystal_lz
 * blog: http://st233.com
 * Gitee: https://gitee.com/DebugST
 * Github: https://github.com/DebugST
 */
namespace ST.Library.UI.NodeEditor
{
    public enum PinStyle { Square, Circle, ArrowUp, ArrowDown, ArrowLeft, ArrowRight }
    public enum PinLocation { Top, Bottom, Left, Right }

    public class STNode
    {
        private STNodeEditor _Owner;
        /// <summary>
        /// Get the current Node owner.
        /// </summary>
        public STNodeEditor Owner {
            get { return _Owner; }
            internal set {
                if (value == _Owner) return;
                if (_Owner != null) {
                    foreach (STNodeOption op in this._InputOptions.ToArray()) op.DisconnectAll();
                    foreach (STNodeOption op in this._OutputOptions.ToArray()) op.DisconnectAll();
                    foreach (STNodeOption op in this.TopOptions.ToArray()) op.DisconnectAll();
                    foreach (STNodeOption op in this.BottomOptions.ToArray()) op.DisconnectAll();
                }
                _Owner = value;
                if (!this._AutoSize) this.SetOptionsLocation();
                this.BuildSize(true, true, false);
                this.OnOwnerChanged();
            }
        }

        private bool _IsSelected;
        /// <summary>
        /// Get or set whether Node is selected.
        /// </summary>
        public bool IsSelected {
            get { return _IsSelected; }
            set {
                if (value == _IsSelected) return;
                _IsSelected = value;
                this.Invalidate();
                this.OnSelectedChanged();
                if (this._Owner != null) this._Owner.OnSelectedChanged(EventArgs.Empty);
            }
        }

        private bool _IsActive;
        /// <summary>
        /// Get whether Node is active.
        /// </summary>
        public bool IsActive {
            get { return _IsActive; }
            internal set {
                if (value == _IsActive) return;
                _IsActive = value;
                this.OnActiveChanged();
            }
        }

        private Color _TitleColor;
        /// <summary>
        /// Get or set the background color of the title.
        /// </summary>
        public Color TitleColor {
            get { return _TitleColor; }
            protected set {
                _TitleColor = value;
                this.Invalidate(new Rectangle(0, 0, this._Width, this._TitleHeight));
            }
        }
        
        private Color _PinAreaColor;
        /// <summary>
        /// Gets or sets the background color for the top and bottom pin areas.
        /// </summary>
        public Color PinAreaColor {
            get { return _PinAreaColor; }
            protected set {
                _PinAreaColor = value;
                this.Invalidate();
            }
        }

        private Color _MarkColor;
        /// <summary>
        /// Get or set the background color of the marker information.
        /// </summary>
        public Color MarkColor {
            get { return _MarkColor; }
            protected set {
                _MarkColor = value;
                this.Invalidate(this._MarkRectangle);
            }
        }

        private Color _ForeColor = Color.White;
        /// <summary>
        /// Get or set the current Node foreground color.
        /// </summary>
        public Color ForeColor {
            get { return _ForeColor; }
            protected set {
                _ForeColor = value;
                this.Invalidate();
            }
        }

        private Color _BackColor;
        /// <summary>
        /// Get or set the background color of the current Node.
        /// </summary>
        public Color BackColor {
            get { return _BackColor; }
            protected set {
                _BackColor = value;
                this.Invalidate();
            }
        }

        private string _Title;
        /// <summary>
        /// Get or set the Node title.
        /// </summary>
        public string Title {
            get { return _Title; }
            protected set {
                _Title = value;
                if (this._AutoSize) this.BuildSize(true, true, true);
                //this.Invalidate(this.TitleRectangle);
            }
        }

        private string _SubTitle;
        /// <summary>
        /// Get or set the Node subtitle.
        /// </summary>
        public string SubTitle
        {
            get { return _SubTitle; }
            protected set
            {
                _SubTitle = value;
                if (this._AutoSize) this.BuildSize(true, true, true);
                //this.Invalidate(this.TitleRectangle);
            }
        }

        private string _Mark;
        /// <summary>
        /// Get or set Node tag information.
        /// </summary>
        public string Mark {
            get { return _Mark; }
            set {
                _Mark = value;
                if (value == null)
                    _MarkLines = null;
                else
                    _MarkLines = (from s in value.Split('\n') select s.Trim()).ToArray();

                this.BuildSize(false, true, false);
                this.Invalidate(new Rectangle(-5, -5, this._MarkRectangle.Width + 10, this._MarkRectangle.Height + 10));
            }
        }

        private string[] _MarkLines;//Store the row data separately, no need to split each time in the drawing.
        /// <summary>
        /// Get the data of the Node tag information row.
        /// </summary>
        public string[] MarkLines {
            get { return _MarkLines; }
        }

        private int _Left;
        /// <summary>
        /// Get or set the left coordinate of Node.
        /// </summary>
        public int Left {
            get { return _Left; }
            set {
                if (this._LockLocation || value == _Left) return;
                _Left = value;
                this.SetOptionsLocation();
                this.BuildSize(false, true, false);
                this.OnMove(EventArgs.Empty);
                if (this._Owner != null) {
                    this._Owner.BuildLinePath();
                    this._Owner.BuildBounds();
                }
            }
        }

        private int _Top;
        /// <summary>
        /// Get or set the top coordinate of Node.
        /// </summary>
        public int Top {
            get { return _Top; }
            set {
                if (this._LockLocation || value == _Top) return;
                _Top = value;
                this.SetOptionsLocation();
                this.BuildSize(false, true, false);
                this.OnMove(EventArgs.Empty);
                if (this._Owner != null) {
                    this._Owner.BuildLinePath();
                    this._Owner.BuildBounds();
                }
            }
        }

        private int _Width = 100;
        /// <summary>
        /// Gets or sets the width of the Node. This value cannot be set when AutoSize is set.
        /// </summary>
        public int Width {
            get { return _Width; }
            protected set {
                if (value < 50) return;
                if (this._AutoSize || value == _Width) return;
                _Width = value;
                this.SetOptionsLocation();
                this.BuildSize(false, true, false);
                this.OnResize(EventArgs.Empty);
                if (this._Owner != null) {
                    this._Owner.BuildLinePath();
                    this._Owner.BuildBounds();
                }
                this.Invalidate();
            }
        }

        private int _Height = 40;
        /// <summary>
        /// Gets or sets the height of Node. This value cannot be set when AutoSize is set.
        /// </summary>
        public int Height {
            get { return _Height; }
            protected set {
                if (value < 40) return;
                if (this._AutoSize || value == _Height) return;
                _Height = value;
                this.SetOptionsLocation();
                this.BuildSize(false, true, false);
                this.OnResize(EventArgs.Empty);
                if (this._Owner != null) {
                    this._Owner.BuildLinePath();
                    this._Owner.BuildBounds();
                }
                this.Invalidate();
            }
        }

        private int _ItemHeight = 20;
        /// <summary>
        /// Get or set the height of each option of Node.
        /// </summary>
        public int ItemHeight {
            get { return _ItemHeight; }
            protected set {
                if (value < 16) value = 16;
                if (value > 200) value = 200;
                if (value == _ItemHeight) return;
                _ItemHeight = value;
                if (this._AutoSize) {
                    this.BuildSize(true, false, true);
                } else {
                    this.SetOptionsLocation();
                    if (this._Owner != null) this._Owner.Invalidate();
                }
            }
        }

        private bool _AutoSize = true;
        /// <summary>
        /// Get or set whether Node automatically calculates width and height.
        /// </summary>
        public bool AutoSize {
            get { return _AutoSize; }
            protected set { _AutoSize = value; }
        }
        /// <summary>
        /// Get the coordinates of the right side of Node.
        /// </summary>
        public int Right {
            get { return _Left + _Width; }
        }
        /// <summary>
        /// Get the coordinates of the bottom of Node.
        /// </summary>
        public int Bottom {
            get { return _Top + _Height; }
        }
        /// <summary>
        /// Get Node rectangle area.
        /// </summary>
        public Rectangle Rectangle {
            get {
                return new Rectangle(this._Left, this._Top, this._Width, this._Height);
            }
        }
        /// <summary>
        /// Get the rectangular area of ​​the Node title.
        /// </summary>
        public Rectangle TitleRectangle {
            get {
                int top_space = (RenderingOptions && this.TopOptions.Count > 0) ? this._ItemHeight : 0;
                return new Rectangle(this._Left, this._Top + top_space, this._Width, this._TitleHeight);
            }
        }

        private Rectangle _MarkRectangle;
        /// <summary>
        /// Get Node marked rectangular area.
        /// </summary>
        public Rectangle MarkRectangle {
            get { return _MarkRectangle; }
        }

        private int _TitleHeight = 20;
        /// <summary>
        /// Get or set the height of the Node title.
        /// </summary>
        public int TitleHeight {
            get { return _TitleHeight; }
            protected set { _TitleHeight = value; }
        }

        private STNodeOptionCollection _InputOptions;
        /// <summary>
        /// Get a collection of input options. (Left pins)
        /// </summary>
        protected internal STNodeOptionCollection InputOptions {
            get { return _InputOptions; }
        }
        /// <summary>
        /// Get the number of input option sets.
        /// </summary>
        public int InputOptionsCount { get { return _InputOptions.Count; } }

        private STNodeOptionCollection _OutputOptions;
        /// <summary>
        /// Get output options. (Right pins)
        /// </summary>
        protected internal STNodeOptionCollection OutputOptions {
            get { return _OutputOptions; }
        }
        /// <summary>
        /// Get the number of output options.
        /// </summary>
        public int OutputOptionsCount { get { return _OutputOptions.Count; } }

        private STNodeOptionCollection _TopOptions;
        /// <summary>
        /// Get a collection of top options. (top pins)
        /// </summary>
        protected internal STNodeOptionCollection TopOptions
        {
            get { return _TopOptions; }
        }
        /// <summary>
        /// Get the number of top option sets.
        /// </summary>
        public int TopOptionsCount { get { return _TopOptions.Count; } }

        private STNodeOptionCollection _BottomOptions;
        /// <summary>
        /// Get bottom options. (bottom pins)
        /// </summary>
        protected internal STNodeOptionCollection BottomOptions
        {
            get { return _BottomOptions; }
        }
        /// <summary>
        /// Get the number of bottom options.
        /// </summary>
        public int BottomOptionsCount { get { return _BottomOptions.Count; } }

        private int _maxPinWidth = 65;
        /// <summary>
        /// Gets or sets the maximum width for an individual top or bottom pin's text area.
        /// When the editor is zoomed, the text will scale to fit and be clipped.
        /// </summary>
        public int MaxPinWidth {
            get { return _maxPinWidth; }
            set {
                if (value < 10) value = 10; // Set a reasonable minimum
                if (_maxPinWidth == value) return;
                _maxPinWidth = value;
                this.BuildSize(true, true, true);
            }
        }

        /// <summary>
        /// Gets or sets a fixed width for the node.
        /// When set, the node will not auto-size its width based on content.
        /// </summary>
        public int? FixedWidth { get; set; }

        private STNodeControlCollection _Controls;
        /// <summary>
        /// Get the collection of controls contained in Node.
        /// </summary>
        protected STNodeControlCollection Controls {
            get { return _Controls; }
        }
        /// <summary>
        /// Get the number of control collections contained in Node.
        /// </summary>
        public int ControlsCount { get { return _Controls.Count; } }
        /// <summary>
        /// Get Node coordinate position.
        /// </summary>
        public Point Location {
            get { return new Point(this._Left, this._Top); }
            set {
                this.Left = value.X;
                this.Top = value.Y;
            }
        }
        /// <summary>
        /// Get Node size.
        /// </summary>
        public Size Size {
            get { return new Size(this._Width, this._Height); }
            set {
                this.Width = value.Width;
                this.Height = value.Height;
            }
        }

        private Font _Font;
        private Font _FontBold;
        /// <summary>
        /// Get or set the Node font.
        /// </summary>
        protected Font Font {
            get { return _Font; }
            set {
                if (value == _Font) return;
                if (this._Font != null) this._Font.Dispose();
                _Font = value;
                _FontBold = new Font(this._Font, FontStyle.Bold);
            }
        }

        private bool _LockOption;
        /// <summary>
        /// Get or set whether to lock the Option option. After locking, it will not accept the connection.
        /// </summary>
        public bool LockOption {
            get { return _LockOption; }
            set {
                _LockOption = value;
                this.Invalidate(new Rectangle(0, 0, this._Width, this._TitleHeight));
            }
        }

        private bool _LockLocation;
        /// <summary>
        /// Gets or sets whether to lock the Node position and cannot move after being locked.
        /// </summary>
        public bool LockLocation {
            get { return _LockLocation; }
            set {
                _LockLocation = value;
                this.Invalidate(new Rectangle(0, 0, this._Width, this._TitleHeight));
            }
        }

        private ContextMenuStrip _ContextMenuStrip;
        /// <summary>
        /// Get or set the current Node context menu.
        /// </summary>
        public ContextMenuStrip ContextMenuStrip {
            get { return _ContextMenuStrip; }
            set { _ContextMenuStrip = value; }
        }

        private object _Tag;
        /// <summary>
        /// Get or set user-defined saved data.
        /// </summary>
        public object Tag {
            get { return _Tag; }
            set { _Tag = value; }
        }

        private Guid _Guid;
        /// <summary>
        /// Get the globally unique identifier.
        /// </summary>
        public Guid Guid {
            get { return _Guid; }
        }

        private Entity _entity = null;
        public Entity Entity
        {
            get { return _entity; }
            set
            {
                _entity = value;
                _shouldRenderOptions = _entity.variant != EntityVariant.VARIABLE;
            }
        }
        public ShortGuid ShortGUID => Entity.shortGUID;

        private bool _shouldRenderOptions = true;
        public bool RenderingOptions => _shouldRenderOptions;

        private bool _LetGetOptions = false;
        /// <summary>
        /// Get or set whether to allow external access to STNodeOption.
        /// </summary>
        public bool LetGetOptions {
            get { return _LetGetOptions; }
            protected set { _LetGetOptions = value; }
        }

        private static Point m_static_pt_init = new Point(10, 10);

        public int NodeID; //This is used at reconstruction time to ensure connections point to the correct nodes. Ignore it elsewhere.

        public STNode() {
            this._Title = "Untitled";
            this._SubTitle = "";
            this._MarkRectangle.Height = this._Height;
            this._Left = this._MarkRectangle.X = m_static_pt_init.X;
            this._Top = m_static_pt_init.Y;
            this._MarkRectangle.Y = this._Top - 30;
            this._InputOptions = new STNodeOptionCollection(this, PinLocation.Left);
            this._OutputOptions = new STNodeOptionCollection(this, PinLocation.Right);
            this._TopOptions = new STNodeOptionCollection(this, PinLocation.Top);
            this._BottomOptions = new STNodeOptionCollection(this, PinLocation.Bottom);
            this._Controls = new STNodeControlCollection(this);
            this._BackColor = Color.FromArgb(200, 64, 64, 64);
            this._TitleColor = Color.FromArgb(200, Color.DodgerBlue);
            this._MarkColor = Color.FromArgb(200, Color.Brown);
            this.PinAreaColor = Color.FromArgb(200, 80, 80, 80);
            this._Font = new Font("courier new", 8.25f);

            FixedWidth = 150;

            m_sf = new StringFormat();
            m_sf.Alignment = StringAlignment.Near;
            m_sf.LineAlignment = StringAlignment.Center;
            m_sf.FormatFlags = StringFormatFlags.NoWrap;
            m_sf.SetTabStops(0, new float[] { 40 });
            m_static_pt_init.X += 10;
            m_static_pt_init.Y += 10;
            this._Guid = Guid.NewGuid();
            this.OnCreate();
        }

        public void Recompute()
        {
            this.SetOptionsLocation();
            this.BuildSize(false, true, false);
            this.OnResize(EventArgs.Empty);
            this.Invalidate();
        }

        public void SetName(string name, string subtitle = "")
        {
            int height = 20;
            if (subtitle != "")
                height = 35;

            Title = name;
            SubTitle = subtitle;
            TitleHeight = height;
        }

        public void SetColour(Color colourTitleBar, Color colourTopBottomPins, Color colourText)
        {
            TitleColor = colourTitleBar;
            PinAreaColor = colourTopBottomPins;
            ForeColor = colourText;
        }

        public void SetPosition(Point location)
        {
            Location = location;
        }

        public STNodeOption AddInputOption(ShortGuid option, bool unique = false)
        {
            if (!unique)
                for (int i = 0; i < this.InputOptions.Count; i++)
                    if (this.InputOptions[i].ShortGUID == option)
                        return this.InputOptions[i];

            var newOp = this.InputOptions.Add(option, typeof(void), false);
            newOp.Style = PinStyle.ArrowRight;
            return newOp;
        }
        public STNodeOption AddOutputOption(ShortGuid option, bool unique = false)
        {
            if (!unique)
                for (int i = 0; i < this.OutputOptions.Count; i++)
                    if (this.OutputOptions[i].ShortGUID == option)
                        return this.OutputOptions[i];

            var newOp = this.OutputOptions.Add(option, typeof(void), false);
            newOp.Style = PinStyle.ArrowRight;
            return newOp;
        }

        public STNodeOption AddTopOption(ShortGuid option, PinStyle style = PinStyle.ArrowDown, bool unique = false)
        {
            if (!unique)
                for (int i = 0; i < this.TopOptions.Count; i++)
                    if (this.TopOptions[i].ShortGUID == option)
                        return this.TopOptions[i];

            var newOp = this.TopOptions.Add(option, typeof(void), false);
            if (style != PinStyle.ArrowUp && style != PinStyle.ArrowDown) style = PinStyle.ArrowUp;
            newOp.Style = style;
            return newOp;
        }
        public STNodeOption AddBottomOption(ShortGuid option, bool unique = false)
        {
            if (!unique)
                for (int i = 0; i < this.BottomOptions.Count; i++)
                    if (this.BottomOptions[i].ShortGUID == option)
                        return this.BottomOptions[i];

            var newOp = this.BottomOptions.Add(option, typeof(void), false);
            newOp.Style = PinStyle.ArrowDown;
            return newOp;
        }

        public STNodeOption GetOption(ShortGuid option)
        {
            for (int i = 0; i < this.InputOptions.Count; i++)
                if (this.InputOptions[i].ShortGUID == option)
                    return this.InputOptions[i];
            for (int i = 0; i < this.OutputOptions.Count; i++)
                if (this.OutputOptions[i].ShortGUID == option)
                    return this.OutputOptions[i];
            for (int i = 0; i < this.TopOptions.Count; i++)
                if (this.TopOptions[i].ShortGUID == option)
                    return this.TopOptions[i];
            for (int i = 0; i < this.BottomOptions.Count; i++)
                if (this.BottomOptions[i].ShortGUID == option)
                    return this.BottomOptions[i];
            return null;
        }

        public void RemoveInputOption(ShortGuid option)
        {
            var inputs = this.InputOptions.ToArray().ToList().FindAll(o => o.ShortGUID == option);
            foreach (var input in inputs)
                this.InputOptions.Remove(input);
        }
        public void RemoveOutputOption(ShortGuid option)
        {
            var inputs = this.OutputOptions.ToArray().ToList().FindAll(o => o.ShortGUID == option);
            foreach (var input in inputs)
                this.OutputOptions.Remove(input);
        }
        public void RemoveTopOption(ShortGuid option)
        {
            var inputs = this.TopOptions.ToArray().ToList().FindAll(o => o.ShortGUID == option);
            foreach (var input in inputs)
                this.TopOptions.Remove(input);
        }
        public void RemoveBottomOption(ShortGuid option)
        {
            var inputs = this.BottomOptions.ToArray().ToList().FindAll(o => o.ShortGUID == option);
            foreach (var input in inputs)
                this.BottomOptions.Remove(input);
        }

        //private int m_nItemHeight = 30;
        protected StringFormat m_sf;
        /// <summary>
        /// Currently active controls in Node.
        /// </summary>
        protected STNodeControl m_ctrl_active;
        /// <summary>
        /// Controls hovering in the current Node.
        /// </summary>
        protected STNodeControl m_ctrl_hover;
        /// <summary>
        /// The control under the mouse click in the current Node.
        /// </summary>
        protected STNodeControl m_ctrl_down;

        protected internal void BuildSize(bool bBuildNode, bool bBuildMark, bool bRedraw) {
            if (this._Owner == null) return;
            using (Graphics g = this._Owner.CreateGraphics()) {
                if (this._AutoSize && bBuildNode) {
                    Size sz = this.GetDefaultNodeSize(g);
                    if (this._Width != sz.Width || this._Height != sz.Height) {
                        this._Width = sz.Width;
                        this._Height = sz.Height;
                        this.SetOptionsLocation();
                        this.OnResize(EventArgs.Empty);
                    }
                }
                if (bBuildMark && !string.IsNullOrEmpty(this._Mark)) {
                    this._MarkRectangle = this.OnBuildMarkRectangle(g);
                }
            }
            if (bRedraw) this._Owner.Invalidate();
        }

        internal Dictionary<string, byte[]> OnSaveNode() {
            Dictionary<string, byte[]> dic = new Dictionary<string, byte[]>();
            dic.Add("Guid", this._Guid.ToByteArray());
            dic.Add("Left", BitConverter.GetBytes(this._Left));
            dic.Add("Top", BitConverter.GetBytes(this._Top));
            dic.Add("Width", BitConverter.GetBytes(this._Width));
            dic.Add("Height", BitConverter.GetBytes(this._Height));
            dic.Add("AutoSize", new byte[] { (byte)(this._AutoSize ? 1 : 0) });
            if (this._Mark != null) dic.Add("Mark", Encoding.UTF8.GetBytes(this._Mark));
            dic.Add("LockOption", new byte[] { (byte)(this._LockLocation ? 1 : 0) });
            dic.Add("LockLocation", new byte[] { (byte)(this._LockLocation ? 1 : 0) });
            Type t = this.GetType();
            foreach (var p in t.GetProperties()) {
                var attrs = p.GetCustomAttributes(true);
                foreach (var a in attrs) {
                    if (!(a is STNodePropertyAttribute)) continue;
                    var attr = a as STNodePropertyAttribute;
                    object obj = Activator.CreateInstance(attr.DescriptorType);
                    if (!(obj is STNodePropertyDescriptor))
                        throw new InvalidOperationException("[STNodePropertyAttribute.Type] The parameter value must be [STNodePropertyDescriptor] or its subclass type.");
                    var desc = (STNodePropertyDescriptor)Activator.CreateInstance(attr.DescriptorType);
                    desc.Node = this;
                    desc.PropertyInfo = p;
                    byte[] byData = desc.GetBytesFromValue();
                    if (byData == null) continue;
                    dic.Add(p.Name, byData);
                }
            }
            this.OnSaveNode(dic);
            return dic;
        }

        internal byte[] GetSaveData() {
            List<byte> lst = new List<byte>();
            Type t = this.GetType();
            byte[] byData = Encoding.UTF8.GetBytes(t.Module.Name + "|" + t.FullName);
            lst.Add((byte)byData.Length);
            lst.AddRange(byData);
            byData = Encoding.UTF8.GetBytes(t.GUID.ToString());
            lst.Add((byte)byData.Length);
            lst.AddRange(byData);

            var dic = this.OnSaveNode();
            if (dic != null) {
                foreach (var v in dic) {
                    byData = Encoding.UTF8.GetBytes(v.Key);
                    lst.AddRange(BitConverter.GetBytes(byData.Length));
                    lst.AddRange(byData);
                    lst.AddRange(BitConverter.GetBytes(v.Value.Length));
                    lst.AddRange(v.Value);
                }
            }
            return lst.ToArray();
        }

        #region protected
        /// <summary>
        /// Occurs when Node is constructed.
        /// </summary>
        protected void OnCreate()
        {
            LetGetOptions = true;
            Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, (byte)0);
        }
        /// <summary>
        /// Draw the entire Node.
        /// </summary>
        /// <param name="dt">Drawing tools</param>
        protected internal virtual void OnDrawNode(DrawingTools dt) {
            dt.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            int top_space = (RenderingOptions && this.TopOptions.Count > 0) ? this._ItemHeight : 0;
            int bottom_space = (RenderingOptions && this.BottomOptions.Count > 0) ? this._ItemHeight : 0;

            // Draw the middle body background
            if (this._BackColor.A != 0 && RenderingOptions) {
                dt.SolidBrush.Color = this._BackColor;
                Rectangle bodyRect = new Rectangle(this.Left, this.Top + top_space, this.Width, this.Height - top_space - bottom_space);
                if (this.Owner.RoundedCornerRadius == -1 || this.BottomOptionsCount != 0)
                {
                    dt.Graphics.FillRectangle(dt.SolidBrush, bodyRect);
                }
                else
                {
                    RoundedCornerUtils.FillRoundedRectangleBottom(dt.Graphics, dt.SolidBrush, bodyRect, Owner.RoundedCornerRadius);
                }
            }

            // Draw top pin area background
            if (top_space > 0) {
                dt.SolidBrush.Color = this.PinAreaColor;
                Rectangle topRect = new Rectangle(this.Left, this.Top, this.Width, top_space);
                if (this.Owner.RoundedCornerRadius == -1) {
                    dt.Graphics.FillRectangle(dt.SolidBrush, topRect);
                } else {
                    RoundedCornerUtils.FillRoundedRectangleTop(dt.Graphics, dt.SolidBrush, topRect, Owner.RoundedCornerRadius, false);
                }
            }

            // Draw bottom pin area background
            if (bottom_space > 0) {
                dt.SolidBrush.Color = this.PinAreaColor;
                Rectangle bottomRect = new Rectangle(this.Left, this.Bottom - bottom_space, this.Width, bottom_space);
                if (this.Owner.RoundedCornerRadius == -1) {
                    dt.Graphics.FillRectangle(dt.SolidBrush, bottomRect);
                } else {
                    RoundedCornerUtils.FillRoundedRectangleBottom(dt.Graphics, dt.SolidBrush, bottomRect, Owner.RoundedCornerRadius);
                }
            }
            
            // Now draw the title and other body elements on top of the backgrounds
            this.OnDrawTitle(dt);
            this.OnDrawBody(dt);
        }
        /// <summary>
        /// Draw the Node header part.
        /// </summary>
        /// <param name="dt">Drawing tools</param>
        protected virtual void OnDrawTitle(DrawingTools dt) {
            Rectangle titleRect = this.TitleRectangle;

            m_sf.Alignment = StringAlignment.Near; // Left-align title text
            m_sf.LineAlignment = StringAlignment.Center;
            Graphics g = dt.Graphics;
            SolidBrush brush = dt.SolidBrush;

            // Draw the title bar background
            if (this._TitleColor.A != 0) {
                brush.Color = this._TitleColor;
                if (this.Owner.RoundedCornerRadius == -1 || (this.TopOptionsCount != 0 && RenderingOptions))
                {
                    g.FillRectangle(brush, this.TitleRectangle);
                }
                else
                {
                    RoundedCornerUtils.FillRoundedRectangleTop(g, brush, this.TitleRectangle, Owner.RoundedCornerRadius, InputOptionsCount + OutputOptionsCount + TopOptionsCount + BottomOptionsCount == 0 || !RenderingOptions);
                }
            }

            // Draw lock icons, adjusted to the new titleRect position
            if (this._LockOption) {
                brush.Color = this._ForeColor;
                int n = titleRect.Y + this._TitleHeight / 2 - 5;
                g.FillRectangle(dt.SolidBrush, this._Left + 4, n + 0, 2, 4);
                g.FillRectangle(dt.SolidBrush, this._Left + 6, n + 0, 2, 2);
                g.FillRectangle(dt.SolidBrush, this._Left + 8, n + 0, 2, 4);
                g.FillRectangle(dt.SolidBrush, this._Left + 3, n + 4, 8, 6);
            }
            if (this._LockLocation) {
                brush.Color = this._ForeColor;
                int n = titleRect.Y + this._TitleHeight / 2 - 5;
                g.FillRectangle(brush, this.Right - 9, n, 4, 4);
                g.FillRectangle(brush, this.Right - 11, n + 4, 8, 2);
                g.FillRectangle(brush, this.Right - 8, n + 6, 2, 4);
            }

            // Create a padded rectangle for the text to achieve left padding and clipping.
            Rectangle textRect = titleRect;
            textRect.X += 10; // 10 pixels of left padding
            textRect.Width -= 15; // Reduce width to account for padding

            // Font scaling logic based on zoom
            Font fontToUse = this._FontBold;
            Font subFontToUse = this._Font;
            bool fontCreated = false;
            
            float zoom = dt.Graphics.Transform.Elements[0];
            if (this.Owner != null && zoom > 1.0f) {
                // When zoomed in, reduce font size to show more text.
                float newSize = this._FontBold.Size / zoom;
                fontToUse = new Font(this._FontBold.FontFamily, newSize, this._FontBold.Style);
                subFontToUse = new Font(this._Font.FontFamily, newSize, this._Font.Style);
                fontCreated = true;
            }
            
            // Set clipping region to avoid text overflowing the title bar
            Region oldClip = g.Clip;
            g.SetClip(textRect, CombineMode.Intersect);

            // Draw Title and Subtitle text
            if (!string.IsNullOrEmpty(this._Title) && this._ForeColor.A != 0) {
                brush.Color = this._ForeColor;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                string title = this._Title;
                if (!string.IsNullOrEmpty(this._SubTitle))
                    title += "\n ";

                g.DrawString(title, fontToUse, brush, textRect, m_sf);
            }
            if (!string.IsNullOrEmpty(this._SubTitle) && this._ForeColor.A != 0)
            {
                brush.Color = this._ForeColor;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                string subTitle = this._SubTitle;
                if (!string.IsNullOrEmpty(this._Title))
                    subTitle = " \n" + subTitle;

                g.DrawString(subTitle, subFontToUse, brush, textRect, m_sf);
            }

            // Restore original clipping region and dispose temporary fonts
            g.SetClip(oldClip, CombineMode.Replace);
            if (fontCreated) {
                fontToUse.Dispose();
                subFontToUse.Dispose();
            }
            
            // Restore StringFormat for other drawing operations that might expect it to be centered.
            m_sf.Alignment = StringAlignment.Center;
        }
        protected virtual void OnDrawBody(DrawingTools dt) {
            int top_space = (RenderingOptions && this.TopOptions.Count > 0) ? this._ItemHeight : 0;
            if (this._Controls.Count != 0) {
                dt.Graphics.TranslateTransform(this._Left, this._Top + top_space + this._TitleHeight);
                Point pt = Point.Empty;
                Point pt_last = Point.Empty;
                foreach (STNodeControl v in this._Controls) {
                    if (!v.Visible) continue;
                    pt.X = v.Left - pt_last.X;
                    pt.Y = v.Top - pt_last.Y;
                    pt_last = v.Location;
                    dt.Graphics.TranslateTransform(pt.X, pt.Y);
                    dt.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    v.OnPaint(dt);
                }
                dt.Graphics.TranslateTransform(-this._Left - pt_last.X, -this._Top - top_space - this._TitleHeight - pt_last.Y);
            }

            foreach (STNodeOption op in this.TopOptions)
            {
                if (op == STNodeOption.Empty) continue;
                this.OnDrawOptionDot(dt, op);
                this.OnDrawOptionText(dt, op);
            }

            foreach (STNodeOption op in this.InputOptions)
            {
                if (op == STNodeOption.Empty) continue;
                this.OnDrawOptionDot(dt, op);
                this.OnDrawOptionText(dt, op);
            }

            foreach (STNodeOption op in this.OutputOptions)
            {
                if (op == STNodeOption.Empty) continue;
                this.OnDrawOptionDot(dt, op);
                this.OnDrawOptionText(dt, op);
            }
            
            foreach (STNodeOption op in this.BottomOptions)
            {
                if (op == STNodeOption.Empty) continue;
                this.OnDrawOptionDot(dt, op);
                this.OnDrawOptionText(dt, op);
            }
        }
        /// <summary>
        /// Draw marker information.
        /// </summary>
        /// <param name="dt">Drawing tools</param>
        protected internal virtual void OnDrawMark(DrawingTools dt) {
            if (string.IsNullOrEmpty(this._Mark)) return;
            Graphics g = dt.Graphics;
            SolidBrush brush = dt.SolidBrush;
            m_sf.LineAlignment = StringAlignment.Center;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            brush.Color = this._MarkColor;
            g.FillRectangle(brush, this._MarkRectangle);                                //Fill background color

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;       //Determine the size required for text drawing
            var sz = g.MeasureString(this.Mark, this.Font, this._MarkRectangle.Width);
            brush.Color = this._ForeColor;
            if (sz.Height > this._ItemHeight || sz.Width > this._MarkRectangle.Width) {    //If it exceeds the drawing area, draw part
                Rectangle rect = new Rectangle(this._MarkRectangle.Left + 2, this._MarkRectangle.Top + 2, this._MarkRectangle.Width - 20, 16);
                m_sf.Alignment = StringAlignment.Near;
                g.DrawString(this._MarkLines[0], this._Font, brush, rect, m_sf);
                m_sf.Alignment = StringAlignment.Far;
                rect.Width = this._MarkRectangle.Width - 5;
                g.DrawString("+", this._Font, brush, rect, m_sf);                       // + Indicates that the drawing area is exceeded
            } else {
                m_sf.Alignment = StringAlignment.Near;
                g.DrawString(this._MarkLines[0].Trim(), this._Font, brush, this._MarkRectangle, m_sf);
            }
        }
        /// <summary>
        /// Draw the points of the option line.
        /// </summary>
        /// <param name="dt">Drawing tools</param>
        /// <param name="op">Specified options</param>
        protected virtual void OnDrawOptionDot(DrawingTools dt, STNodeOption op) {
            Graphics g = dt.Graphics;
            SolidBrush brush = dt.SolidBrush;
            var t = typeof(object);
            if (op.DotColor != Color.Transparent)           //Set color
                brush.Color = op.DotColor;
            else
            {
                if (op.DataType == t)
                    brush.Color = this.Owner.UnknownTypeColor;
                else
                    brush.Color = this.Owner.TypeColor.ContainsKey(op.DataType) ? this.Owner.TypeColor[op.DataType] : this.Owner.UnknownTypeColor;
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            Point[] points;
            Rectangle r = op.DotRectangle;

            switch (op.Style)
            {
                case PinStyle.ArrowUp:
                    points = new Point[] {
                        new Point(r.Left, r.Bottom),
                        new Point(r.Right, r.Bottom),
                        new Point(r.X + r.Width / 2, r.Top)
                    };
                    g.FillPolygon(brush, points);
                    break;

                case PinStyle.ArrowDown:
                    points = new Point[] {
                        new Point(r.Left, r.Top),
                        new Point(r.Right, r.Top),
                        new Point(r.X + r.Width / 2, r.Bottom)
                    };
                    g.FillPolygon(brush, points);
                    break;

                case PinStyle.ArrowLeft:
                    points = new Point[] {
                        new Point(r.Right, r.Top),
                        new Point(r.Right, r.Bottom),
                        new Point(r.Left, r.Y + r.Height / 2)
                    };
                    g.FillPolygon(brush, points);
                    break;

                case PinStyle.ArrowRight:
                    points = new Point[] {
                       new Point(r.Left, r.Top),
                       new Point(r.Left, r.Bottom),
                       new Point(r.Right, r.Y + r.Height / 2)
                   };
                    g.FillPolygon(brush, points);
                    break;

                case PinStyle.Circle:
                    g.FillEllipse(brush, r);
                    break;

                case PinStyle.Square:
                default:
                    g.SmoothingMode = SmoothingMode.None;
                    g.FillRectangle(brush, r);
                    break;
            }
        }
        protected virtual void OnDrawOptionText(DrawingTools dt, STNodeOption op)
        {
            if (!RenderingOptions)
                return;

            Graphics g = dt.Graphics;
            SolidBrush brush = dt.SolidBrush;
            
            bool isHorizontalPin = this.TopOptions.Contains(op) || this.BottomOptions.Contains(op);
            
            if (isHorizontalPin) {
                m_sf.Alignment = StringAlignment.Center;
                m_sf.LineAlignment = op.Location == PinLocation.Top ? StringAlignment.Near : StringAlignment.Far;
            } else {
                m_sf.Alignment = op.Location == PinLocation.Left ? StringAlignment.Near : StringAlignment.Far;
                m_sf.LineAlignment = StringAlignment.Center;
            }
            
            RectangleF textRect = op.TextRectangle;
            Font fontToUse = this.Font;
            bool fontCreated = false;

            // For horizontal pins, dynamically adjust the font size so the text fits within the pin's capped width.
            if (isHorizontalPin) {
                // Measure the full text width with the node's default font.
                SizeF fullTextSize = g.MeasureString(op.Text, this.Font);
                
                // The visible width is defined by the pin's text rectangle.
                float visibleWidth = textRect.Width;

                // If the full text is wider than the allowed space, we need to create a new, smaller font.
                if (fullTextSize.Width > visibleWidth && visibleWidth > 0) {
                    // Calculate the ratio to scale the font size.
                    float scaleRatio = visibleWidth / fullTextSize.Width;
                    float newSize = this.Font.Size * scaleRatio;
                    
                    // Create the new font. We'll dispose of it after drawing.
                    fontToUse = new Font(this.Font.FontFamily, newSize, this.Font.Style);
                    fontCreated = true;
                }
            }

            brush.Color = op.TextColor;
            g.DrawString(op.Text, fontToUse, brush, textRect, m_sf);
            
            if (fontCreated) {
                fontToUse.Dispose();
            }
            
            m_sf.LineAlignment = StringAlignment.Center;
        }
        protected virtual Point OnSetOptionDotLocation(STNodeOption op, Point pt, int nIndex) {
            return pt;
        }
        protected virtual Rectangle OnSetOptionTextRectangle(STNodeOption op, Rectangle rect, int nIndex) {
            return rect;
        }
        protected virtual Size GetDefaultNodeSize(Graphics g) {
            int nInputHeight = 0, nOutputHeight = 0;
            if (RenderingOptions)
            {
                foreach (STNodeOption op in this._InputOptions) nInputHeight += this._ItemHeight;
                foreach (STNodeOption op in this._OutputOptions) nOutputHeight += this._ItemHeight;
            }

            int top_space = (RenderingOptions && this.TopOptions.Count > 0) ? this._ItemHeight : 0;

            int titleSectionHeight = 20;
            if (!string.IsNullOrEmpty(this._SubTitle)) titleSectionHeight = 35;
            this.TitleHeight = titleSectionHeight; // TitleHeight is now just for the title itself.

            int nHeight = top_space + this._TitleHeight + Math.Max(nInputHeight, nOutputHeight);
            if (RenderingOptions && this.BottomOptions.Count > 0)
            {
                nHeight += this._ItemHeight;
            }
            
            const int H_PADDING = 15;

            float topPinsWidth = 0;
            if (RenderingOptions) {
                foreach (STNodeOption op in this.TopOptions) {
                    float textWidth = g.MeasureString(op.Text, this.Font).Width;
                    topPinsWidth += Math.Min(textWidth, this.MaxPinWidth) + H_PADDING;
                }
            }
            if (topPinsWidth > 0) topPinsWidth -= H_PADDING; // Remove last padding
            
            float bottomPinsWidth = 0;
            if (RenderingOptions) {
                foreach (STNodeOption op in this.BottomOptions) {
                    float textWidth = g.MeasureString(op.Text, this.Font).Width;
                    bottomPinsWidth += Math.Min(textWidth, this.MaxPinWidth) + H_PADDING;
                }
            }
            if (bottomPinsWidth > 0) bottomPinsWidth -= H_PADDING; // Remove last padding
            
            // Get width from Left/Right pin text
            SizeF szf_input = SizeF.Empty, szf_output = SizeF.Empty;
            if (RenderingOptions)
            {
                foreach (STNodeOption v in this._InputOptions) {
                    if (string.IsNullOrEmpty(v.Text)) continue;
                    SizeF szf = g.MeasureString(v.Text, this._Font);
                    if (szf.Width > szf_input.Width) szf_input = szf;
                }
                foreach (STNodeOption v in this._OutputOptions) {
                    if (string.IsNullOrEmpty(v.Text)) continue;
                    SizeF szf = g.MeasureString(v.Text, this._Font);
                    if (szf.Width > szf_output.Width) szf_output = szf;
                }
            }
            int verticalPinTextWidth = (int)(szf_input.Width + szf_output.Width + 25);
            
            // Get width from title
            int titleWidth = 0;
            if (!string.IsNullOrEmpty(this.Title)) {
                 titleWidth = (int)g.MeasureString(this.Title, this._FontBold).Width;
            }
            if (!string.IsNullOrEmpty(this._SubTitle)) {
                int subtitleWidth = (int)g.MeasureString(this._SubTitle, this.Font).Width;
                if (subtitleWidth > titleWidth) titleWidth = subtitleWidth;
            }
            titleWidth += 40; // Padding for title
            
            // Final width is the maximum of all calculated widths
            int nWidth = (int)Math.Max(Math.Max(topPinsWidth, bottomPinsWidth), verticalPinTextWidth);
            if (titleWidth > nWidth) nWidth = titleWidth;
            
            if (this.FixedWidth.HasValue) {
                nWidth = this.FixedWidth.Value;
            }
            
            return new Size(nWidth < 50 ? 50 : nWidth, nHeight);
        }
        /// <summary>
        /// Calculate the rectangular area required by the current Mark.
        /// The returned size does not limit the drawing area, it can still be drawn outside this area.
        /// But it will not be accepted by STNodeEditor and trigger the corresponding event.
        /// </summary>
        /// <param name="g">Drawing panel</param>
        /// <returns>Calculated area</returns>
        protected virtual Rectangle OnBuildMarkRectangle(Graphics g) {
            //if (string.IsNullOrEmpty(this._Mark)) return Rectangle.Empty;
            return new Rectangle(this._Left, this._Top - 30, this._Width, 20);
        }
        /// <summary>
        /// When it needs to be saved, what data does this Node need to save additionally?
        /// Note: Serialization will not be performed when saving, but this Node will only be recreated through the empty parameter constructor when restoring
        ///       Then call OnLoadNode() to restore the saved data
        /// </summary>
        /// <param name="dic">Data to be saved</param>
        protected virtual void OnSaveNode(Dictionary<string, byte[]> dic) { }
        /// <summary>
        /// When the node is restored, the data returned by OnSaveNode() will be re-passed into this function
        /// </summary>
        /// <param name="dic">Data at the time of saving</param>
        protected internal virtual void OnLoadNode(Dictionary<string, byte[]> dic) {
            if (dic.ContainsKey("AutoSize")) this._AutoSize = dic["AutoSize"][0] == 1;
            if (dic.ContainsKey("LockOption")) this._LockOption = dic["LockOption"][0] == 1;
            if (dic.ContainsKey("LockLocation")) this._LockLocation = dic["LockLocation"][0] == 1;
            if (dic.ContainsKey("Guid")) this._Guid = new Guid(dic["Guid"]);
            if (dic.ContainsKey("Left")) this._Left = BitConverter.ToInt32(dic["Left"], 0);
            if (dic.ContainsKey("Top")) this._Top = BitConverter.ToInt32(dic["Top"], 0);
            if (dic.ContainsKey("Width") && !this._AutoSize) this._Width = BitConverter.ToInt32(dic["Width"], 0);
            if (dic.ContainsKey("Height") && !this._AutoSize) this._Height = BitConverter.ToInt32(dic["Height"], 0);
            if (dic.ContainsKey("Mark")) this.Mark = Encoding.UTF8.GetString(dic["Mark"]);
            Type t = this.GetType();
            foreach (var p in t.GetProperties()) {
                var attrs = p.GetCustomAttributes(true);
                foreach (var a in attrs) {
                    if (!(a is STNodePropertyAttribute)) continue;
                    var attr = a as STNodePropertyAttribute;
                    object obj = Activator.CreateInstance(attr.DescriptorType);
                    if (!(obj is STNodePropertyDescriptor))
                        throw new InvalidOperationException("[STNodePropertyAttribute.Type] The parameter value must be [STNodePropertyDescriptor] or its subclass type.");
                    var desc = (STNodePropertyDescriptor)Activator.CreateInstance(attr.DescriptorType);
                    desc.Node = this;
                    desc.PropertyInfo = p;
                    try {
                        if (dic.ContainsKey(p.Name)) desc.SetValue(dic[p.Name]);
                    } catch (Exception ex) {
                        string strErr = "The value of attribute [" + this.Title + "." + p.Name + "] cannot be restored. You can rewrite [STNodePropertyAttribute.GetBytesFromValue(),STNodePropertyAttribute.GetValueFromBytes(byte[])] to ensure that the binary data is correct when saving and loading.";
                        Exception e = ex;
                        while (e != null) {
                            strErr += "\r\n----\r\n[" + e.GetType().Name + "] -> " + e.Message;
                            e = e.InnerException;
                        }
                        throw new InvalidOperationException(strErr, ex);
                    }
                }
            }
        }
        /// <summary>
        /// Occurs when the editor has loaded all nodes
        /// </summary>
        protected internal virtual void OnEditorLoadCompleted() { }
        /// <summary>
        /// Set option text information
        /// </summary>
        /// <param name="op">Target Option</param>
        /// <param name="strText">Text</param>
        /// <returns>whether succeed</returns>
        protected bool SetOptionText(STNodeOption op, ShortGuid shortGUID) {
            if (op.Owner != this) return false;
            op.ShortGUID = shortGUID;
            return true;
        }
        /// <summary>
        /// Set option text information color
        /// </summary>
        /// <param name="op">Target Option</param>
        /// <param name="clr">Color</param>
        /// <returns>Result</returns>
        protected bool SetOptionTextColor(STNodeOption op, Color clr) {
            if (op.Owner != this) return false;
            op.TextColor = clr;
            return true;
        }
        /// <summary>
        /// Set Option connection point color
        /// </summary>
        /// <param name="op">Target Option</param>
        /// <param name="clr">Color</param>
        /// <returns>Result</returns>
        protected bool SetOptionDotColor(STNodeOption op, Color clr) {
            if (op.Owner != this) return false;
            op.DotColor = clr;
            return false;
        }

        protected internal void ClearActiveCtrl()
        {
            if (m_ctrl_active != null)
            {
                m_ctrl_active.OnLostFocus(EventArgs.Empty);
                m_ctrl_active = null;
            }

            if (m_ctrl_hover != null)
            {
                m_ctrl_hover.OnMouseLeave(EventArgs.Empty);
                m_ctrl_hover = null;
            }
        }

        //[event]===========================[event]==============================[event]============================[event]

        protected internal virtual void OnGotFocus(EventArgs e) { }

        protected internal virtual void OnLostFocus(EventArgs e) { }

        protected internal virtual void OnMouseEnter(EventArgs e) { }

        protected internal virtual void OnMouseDown(MouseEventArgs e) {
            Point pt = e.Location;
            int top_space = (RenderingOptions && this.TopOptions.Count > 0) ? this._ItemHeight : 0;
            pt.Y -= (top_space + this._TitleHeight);

            for (int i = this._Controls.Count - 1; i >= 0; i--) {
                var c = this._Controls[i];
                if (c.DisplayRectangle.Contains(pt)) {
                    if (!c.Enabled) return;
                    if (!c.Visible) continue;
                    c.OnMouseDown(new MouseEventArgs(e.Button, e.Clicks, e.X - c.Left, pt.Y - c.Top, e.Delta));
                    m_ctrl_down = c;
                    if (m_ctrl_active != c) {
                        c.OnGotFocus(EventArgs.Empty);
                        if (m_ctrl_active != null) m_ctrl_active.OnLostFocus(EventArgs.Empty);
                        m_ctrl_active = c;
                    }
                    return;
                }
            }
            if (m_ctrl_active != null) m_ctrl_active.OnLostFocus(EventArgs.Empty);
            m_ctrl_active = null;
        }

        protected internal virtual void OnMouseMove(MouseEventArgs e) {
            Point pt = e.Location;
            int top_space = (RenderingOptions && this.TopOptions.Count > 0) ? this._ItemHeight : 0;
            pt.Y -= (top_space + this._TitleHeight);

            if (m_ctrl_down != null) {
                if (m_ctrl_down.Enabled && m_ctrl_down.Visible)
                    m_ctrl_down.OnMouseMove(new MouseEventArgs(e.Button, e.Clicks, e.X - m_ctrl_down.Left, pt.Y - m_ctrl_down.Top, e.Delta));
                return;
            }
            for (int i = this._Controls.Count - 1; i >= 0; i--) {
                var c = this._Controls[i];
                if (c.DisplayRectangle.Contains(pt)) {
                    if (m_ctrl_hover != this._Controls[i]) {
                        c.OnMouseEnter(EventArgs.Empty);
                        if (m_ctrl_hover != null) m_ctrl_hover.OnMouseLeave(EventArgs.Empty);
                        m_ctrl_hover = c;
                    }
                    m_ctrl_hover.OnMouseMove(new MouseEventArgs(e.Button, e.Clicks, e.X - c.Left, pt.Y - c.Top, e.Delta));
                    return;
                }
            }
            if (m_ctrl_hover != null) m_ctrl_hover.OnMouseLeave(EventArgs.Empty);
            m_ctrl_hover = null;
        }

        protected internal virtual void OnMouseUp(MouseEventArgs e) {
            Point pt = e.Location;
            int top_space = (RenderingOptions && this.TopOptions.Count > 0) ? this._ItemHeight : 0;
            pt.Y -= (top_space + this._TitleHeight);

            if (m_ctrl_down != null && m_ctrl_down.Enabled && m_ctrl_down.Visible) {
                m_ctrl_down.OnMouseUp(new MouseEventArgs(e.Button, e.Clicks, e.X - m_ctrl_down.Left, pt.Y - m_ctrl_down.Top, e.Delta));
            }
            m_ctrl_down = null;
        }

        protected internal virtual void OnMouseLeave(EventArgs e) {
            if (m_ctrl_hover != null && m_ctrl_hover.Enabled && m_ctrl_hover.Visible) m_ctrl_hover.OnMouseLeave(e);
            m_ctrl_hover = null;
        }

        protected internal virtual void OnMouseClick(MouseEventArgs e) {
            Point pt = e.Location;
            int top_space = (RenderingOptions && this.TopOptions.Count > 0) ? this._ItemHeight : 0;
            pt.Y -= (top_space + this._TitleHeight);

            if (m_ctrl_active != null && m_ctrl_active.Enabled && m_ctrl_active.Visible)
                m_ctrl_active.OnMouseClick(new MouseEventArgs(e.Button, e.Clicks, e.X - m_ctrl_active.Left, pt.Y - m_ctrl_active.Top, e.Delta));
        }

        protected internal virtual void OnMouseWheel(MouseEventArgs e) {
            Point pt = e.Location;
            int top_space = (RenderingOptions && this.TopOptions.Count > 0) ? this._ItemHeight : 0;
            pt.Y -= (top_space + this._TitleHeight);

            if (m_ctrl_hover != null && m_ctrl_active != null && m_ctrl_active.Enabled && m_ctrl_hover.Visible) {
                m_ctrl_hover.OnMouseWheel(new MouseEventArgs(e.Button, e.Clicks, e.X - m_ctrl_hover.Left, pt.Y - m_ctrl_hover.Top, e.Delta));
                return;
            }
        }
        protected internal virtual void OnMouseHWheel(MouseEventArgs e) {
            if (m_ctrl_hover != null && m_ctrl_active.Enabled && m_ctrl_hover.Visible) {
                m_ctrl_hover.OnMouseHWheel(e);
                return;
            }
        }

        protected internal virtual void OnKeyDown(KeyEventArgs e) {
            if (m_ctrl_active != null && m_ctrl_active.Enabled && m_ctrl_active.Visible) m_ctrl_active.OnKeyDown(e);
        }
        protected internal virtual void OnKeyUp(KeyEventArgs e) {
            if (m_ctrl_active != null && m_ctrl_active.Enabled && m_ctrl_active.Visible) m_ctrl_active.OnKeyUp(e);
        }
        protected internal virtual void OnKeyPress(KeyPressEventArgs e) {
            if (m_ctrl_active != null && m_ctrl_active.Enabled && m_ctrl_active.Visible) m_ctrl_active.OnKeyPress(e);
        }

        protected virtual void OnMove(EventArgs e) { /*this.SetOptionLocation();*/ }
        protected virtual void OnResize(EventArgs e) { /*this.SetOptionLocation();*/ }


        /// <summary>
        /// Occurs when the owner changes.
        /// </summary>
        protected virtual void OnOwnerChanged() { }
        /// <summary>
        /// Occurs when the selected state changes.
        /// </summary>
        protected virtual void OnSelectedChanged() { }
        /// <summary>
        /// Occurs when the activity state changes.
        /// </summary>
        protected virtual void OnActiveChanged() { }

        #endregion protected
        /// <summary>
        /// Calculate the position of each option.
        /// </summary>
        protected virtual void SetOptionsLocation() {
            if (Owner == null) return;

            int top_space = (RenderingOptions && this.TopOptions.Count > 0) ? this._ItemHeight : 0;
            int body_start_y = this._Top + top_space + this._TitleHeight;

            if (RenderingOptions)
            {
                int nIndex = 0;
                Rectangle rect_in = new Rectangle(this.Left, body_start_y, this._Width, this._ItemHeight);
                foreach (STNodeOption op in this._InputOptions) {
                    if (op != STNodeOption.Empty) {
                        int x = this.Left - op.DotSize / 2;
                        if (op.Style == PinStyle.ArrowLeft || op.Style == PinStyle.ArrowRight) {
                            x = this.Left - op.DotSize;
                        }
                        Point pt = this.OnSetOptionDotLocation(op, new Point(x, rect_in.Y + (rect_in.Height - op.DotSize) / 2), nIndex);
                        
                        Rectangle textRect = new Rectangle(this.Left + 10, rect_in.Y, this._Width - 20, this._ItemHeight);
                        if (op.Style == PinStyle.ArrowLeft || op.Style == PinStyle.ArrowRight) {
                             textRect.X = this.Left + 4;
                        }

                        op.TextRectangle = this.OnSetOptionTextRectangle(op, textRect, nIndex);
                        op.DotLeft = pt.X;
                        op.DotTop = pt.Y;
                    }
                    rect_in.Y += this._ItemHeight;
                    nIndex++;
                }
                
                Rectangle rect_out = new Rectangle(this.Left, body_start_y, this._Width, this._ItemHeight);
                nIndex = 0;
                foreach (STNodeOption op in this._OutputOptions) {
                    if (op != STNodeOption.Empty) {
                        int x = this.Right - op.DotSize / 2;
                        if (op.Style == PinStyle.ArrowRight || op.Style == PinStyle.ArrowLeft) {
                            x = this.Right;
                        }
                        Point pt = this.OnSetOptionDotLocation(op, new Point(x, rect_out.Y + (rect_out.Height - op.DotSize) / 2), nIndex);
                        
                        Rectangle textRect = new Rectangle(this.Left + 10, rect_out.Y, this._Width - 20, this._ItemHeight);
                        if (op.Style == PinStyle.ArrowRight || op.Style == PinStyle.ArrowLeft) {
                             textRect.Width = this._Width - 14;
                        }

                        op.TextRectangle = this.OnSetOptionTextRectangle(op, textRect, nIndex);
                        op.DotLeft = pt.X;
                        op.DotTop = pt.Y;
                    }
                    rect_out.Y += this._ItemHeight;
                    nIndex++;
                }
            }
            else
            {
                // When not rendering options, pins should be vertically centered on the title bar.
                int nCenterY = this.TitleRectangle.Y + this.TitleRectangle.Height / 2;
                int nIndex = 0;

                foreach (STNodeOption op in this._InputOptions) {
                    if (op == STNodeOption.Empty) continue;
                    
                    int x = this.Left - op.DotSize / 2;
                    if (op.Style == PinStyle.ArrowLeft || op.Style == PinStyle.ArrowRight) {
                        x = this.Left - op.DotSize;
                    }

                    // Center the dot on the Y axis of the title bar
                    Point pt = this.OnSetOptionDotLocation(op, new Point(x, nCenterY - op.DotSize / 2), nIndex);
                    op.DotLeft = pt.X;
                    op.DotTop = pt.Y;
                    op.TextRectangle = Rectangle.Empty; // No text is rendered
                    nIndex++;
                }

                nIndex = 0;
                foreach (STNodeOption op in this._OutputOptions) {
                    if (op == STNodeOption.Empty) continue;
                    
                    int x = this.Right - op.DotSize / 2;
                    if (op.Style == PinStyle.ArrowRight || op.Style == PinStyle.ArrowLeft) {
                        x = this.Right;
                    }

                    // Center the dot on the Y axis of the title bar
                    Point pt = this.OnSetOptionDotLocation(op, new Point(x, nCenterY - op.DotSize / 2), nIndex);
                    op.DotLeft = pt.X;
                    op.DotTop = pt.Y;
                    op.TextRectangle = Rectangle.Empty; // No text is rendered
                    nIndex++;
                }
            }
            
            const int H_PADDING = 15;
            const int V_PADDING = 2;

            using (var g = this.Owner.CreateGraphics())
            {
                float totalTopWidth = this.TopOptions.Cast<STNodeOption>().Sum(op => Math.Min(g.MeasureString(op.Text, this.Font).Width, this.MaxPinWidth) + H_PADDING);
                if (totalTopWidth > 0) totalTopWidth -= H_PADDING;
                float currentX = this.Left + (this.Width - totalTopWidth) / 2f;
                
                foreach(STNodeOption op in this.TopOptions)
                {
                    if (op == STNodeOption.Empty) continue;
                    float pinTextWidth = g.MeasureString(op.Text, this.Font).Width;
                    float pinVisibleWidth = Math.Min(pinTextWidth, this.MaxPinWidth);
                    
                    int y = this.Top + this._ItemHeight / 2 - op.DotSize / 2;
                    if (op.Style == PinStyle.ArrowUp || op.Style == PinStyle.ArrowDown) 
                        y = this.Top - this._ItemHeight + op.DotSize;

                    op.DotLeft = (int)(currentX + (pinVisibleWidth / 2f) - (op.DotSize / 2f));
                    op.DotTop = y;

                    op.TextRectangle = new Rectangle((int)currentX, this.Top + V_PADDING, (int)pinVisibleWidth, this._ItemHeight);
                    currentX += pinVisibleWidth + H_PADDING;
                }
                
                float totalBottomWidth = this.BottomOptions.Cast<STNodeOption>().Sum(op => Math.Min(g.MeasureString(op.Text, this.Font).Width, this.MaxPinWidth) + H_PADDING);
                if (totalBottomWidth > 0) totalBottomWidth -= H_PADDING;
                currentX = this.Left + (this.Width - totalBottomWidth) / 2f;
                
                foreach(STNodeOption op in this.BottomOptions)
                {
                    if (op == STNodeOption.Empty) continue;
                    float pinTextWidth = g.MeasureString(op.Text, this.Font).Width;
                    float pinVisibleWidth = Math.Min(pinTextWidth, this.MaxPinWidth);

                    int y = this.Bottom - op.DotSize / 2;
                    if (op.Style == PinStyle.ArrowDown || op.Style == PinStyle.ArrowUp) 
                        y = this.Bottom; //todo ; this is wrong

                    op.DotLeft = (int)(currentX + (pinVisibleWidth / 2f) - (op.DotSize / 2f));
                    op.DotTop = y;

                    op.TextRectangle = new Rectangle((int)currentX, this.Bottom - this._ItemHeight, (int)pinVisibleWidth, this._ItemHeight - V_PADDING);
                    currentX += pinVisibleWidth + H_PADDING;
                }
            }
        }

        public void Invalidate() {
            if (this._Owner != null) {
                this._Owner.Invalidate(this._Owner.CanvasToControl(new Rectangle(this._Left - 10, this._Top - 30, this._Width + 20, this._Height + 60)));
            }
        }
        public void Invalidate(Rectangle rect) {
            rect.X += this._Left;
            rect.Y += this._Top;
            if (this._Owner != null) {
                rect = this._Owner.CanvasToControl(rect);
                rect.Width += 1; rect.Height += 1;
                this._Owner.Invalidate(rect);
            }
        }
        public STNodeOption[] GetInputOptions() {
            if (!this._LetGetOptions) return null;
            STNodeOption[] ops = new STNodeOption[this._InputOptions.Count];
            for (int i = 0; i < this._InputOptions.Count; i++) ops[i] = this._InputOptions[i];
            return ops;
        }
        public STNodeOption[] GetOutputOptions() {
            if (!this._LetGetOptions) return null;
            STNodeOption[] ops = new STNodeOption[this._OutputOptions.Count];
            for (int i = 0; i < this._OutputOptions.Count; i++) ops[i] = this._OutputOptions[i];
            return ops;
        }
        public STNodeOption[] GetTopOptions() {
            if (!this._LetGetOptions) return null;
            STNodeOption[] ops = new STNodeOption[this._TopOptions.Count];
            for (int i = 0; i < this._TopOptions.Count; i++) ops[i] = this._TopOptions[i];
            return ops;
        }
        public STNodeOption[] GetBottomOptions() {
            if (!this._LetGetOptions) return null;
            STNodeOption[] ops = new STNodeOption[this._BottomOptions.Count];
            for (int i = 0; i < this._BottomOptions.Count; i++) ops[i] = this._BottomOptions[i];
            return ops;
        }
        public void SetSelected(bool bSelected, bool bRedraw) {
            if (this._IsSelected == bSelected) return;
            this._IsSelected = bSelected;
            if (this._Owner != null) {
                if (bSelected)
                    this._Owner.AddSelectedNode(this);
                else
                    this._Owner.RemoveSelectedNode(this);
            }
            if (bRedraw) this.Invalidate();
            this.OnSelectedChanged();
            if (this._Owner != null) this._Owner.OnSelectedChanged(EventArgs.Empty);
        }
        public IAsyncResult BeginInvoke(Delegate method) { return this.BeginInvoke(method, null); }
        public IAsyncResult BeginInvoke(Delegate method, params object[] args) {
            if (this._Owner == null) return null;
            return this._Owner.BeginInvoke(method, args);
        }
        public object Invoke(Delegate method) { return this.Invoke(method, null); }
        public object Invoke(Delegate method, params object[] args) {
            if (this._Owner == null) return null;
            return this._Owner.Invoke(method, args);
        }
    }
}
