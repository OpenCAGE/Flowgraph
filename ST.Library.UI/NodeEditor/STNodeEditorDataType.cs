using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing;

namespace ST.Library.UI.NodeEditor
{
    public enum ConnectionStatus
    {
        /// <summary>
        /// No owner
        /// </summary>
        [Description("No owner")]
        NoOwner,
        /// <summary>
        /// Same owner
        /// </summary>
        [Description("Same owner")]
        SameOwner,
        /// <summary>
        /// Both are input or output options
        /// </summary>
        [Description("Both are input or output options")]
        SameInputOrOutput,
        /// <summary>
        /// The logic flow doesn't make sense here
        /// </summary>
        [Description("The logic flow doesn't make sense here")]
        InvalidLogicFlow,
        /// <summary>
        /// Different data types
        /// </summary>
        [Description("Different data types")]
        ErrorType,
        /// <summary>
        /// Single connection node
        /// </summary>
        [Description("Single connection node")]
        SingleOption,
        /// <summary>
        /// Circular path
        /// </summary>
        [Description("Circular path")]
        Loop,
        /// <summary>
        /// Existing connection
        /// </summary>
        [Description("Existing connection")]
        Exists,
        /// <summary>
        /// Empty option
        /// </summary>
        [Description("Empty option")]
        EmptyOption,
        /// <summary>
        /// Already connected
        /// </summary>
        [Description("Connected")]
        Connected,
        /// <summary>
        /// Disconnected
        /// </summary>
        [Description("Disconnected")]
        Disconnected,
        /// <summary>
        /// Node is locked
        /// </summary>
        [Description("Node is locked")]
        Locked,
        /// <summary>
        /// Operation denied
        /// </summary>
        [Description("Operation denied")]
        Reject,
        /// <summary>
        /// Connecting
        /// </summary>
        [Description("Connecting")]
        Connecting,
        /// <summary>
        /// Disconnecting
        /// </summary>
        [Description("Disconnecting")]
        Disconnecting
    }

    public enum AlertLocation
    {
        Left,
        Top,
        Right,
        Bottom,
        Center,
        LeftTop,
        RightTop,
        RightBottom,
        LeftBottom,
    }

    public struct DrawingTools
    {
        public Graphics Graphics;
        public Pen Pen;
        public SolidBrush SolidBrush;
    }

    public enum CanvasMoveArgs      //Parameters required when moving the canvas View->MoveCanvas()
    {
        Left = 1,                   //Means to move only the X coordinate
        Top = 2,                    //Means to move only the Y coordinate
        All = 4                     //Means X Y moves at the same time
    }

    public struct NodeFindInfo
    {
        public STNode Node;
        public STNodeOption NodeOption;
        public string Mark;
        public string[] MarkLines;
    }

    public struct ConnectionInfo
    {
        public STNodeOption Input;
        public STNodeOption Output;
        /// <summary>Axis-aligned bounds of the cubic bezier (control-point hull), inflated for hit-testing.</summary>
        public RectangleF HitBounds;
        public PointF P0, P1, P2, P3;
    }

    /// <summary>
    /// OpenCAGE: how active a connected line is, as STNodeEditor.ConnectionActivityProvider reports it.
    /// The line runs from the provider's first option (its start) to its second (its end).
    /// </summary>
    public struct ConnectionActivity
    {
        /// <summary>0..1: how recently the link was active (1 = just now); 0 = not active now.</summary>
        public float Glow;
        /// <summary>It has been active since the host last cleared (drawn dimly in the activity colour when not glowing).</summary>
        public bool Lit;
        /// <summary>Activity flows from the end option back to the start option (data links).</summary>
        public bool Reverse;
        /// <summary>
        /// The colour the line lights up in: its glow, the line itself, the dim "has been active" line, and a light
        /// tint of it for the dashes. Color.Empty (the default) is the editor's ActivityColor.
        /// </summary>
        public Color Color;
        /// <summary>Not active: the line is drawn as usual.</summary>
        public static readonly ConnectionActivity None;

        /// <summary>A line's activity, in the editor's ActivityColor: see Glow, Lit and Reverse.</summary>
        public ConnectionActivity(float glow, bool lit, bool reverse) : this(glow, lit, reverse, Color.Empty) { }

        /// <summary>A line's activity: see Glow, Lit, Reverse and Color.</summary>
        public ConnectionActivity(float glow, bool lit, bool reverse, Color color) {
            this.Glow = glow;
            this.Lit = lit;
            this.Reverse = reverse;
            this.Color = color;
        }
    }

    /// <summary>
    /// OpenCAGE: a short note drawn beside a left or right option's pin, as STNodeEditor.OptionBadgeProvider reports
    /// it (the live link's count of how many times a relay fired or a method was called): between the pin and the
    /// label - "[n] label" on the left, "label [n]" on the right. The node keeps its size and pins; the label is cut
    /// short with an ellipsis when it no longer fits, never the badge.
    /// </summary>
    public struct OptionBadge
    {
        /// <summary>What to show, e.g. "[3]"; null or empty: no badge.</summary>
        public string Text;
        /// <summary>Above 0 (e.g. how recently it changed, 1 = just now): drawn bright; 0: muted. Two states, not a fade.</summary>
        public float Glow;
        /// <summary>The badge's colour; Color.Empty (the default) is the editor's ActivityColor.</summary>
        public Color Color;
        /// <summary>No badge: the option is drawn as usual.</summary>
        public static readonly OptionBadge None;

        /// <summary>A badge: see Text, Glow and Color.</summary>
        public OptionBadge(string text, float glow, Color color) {
            this.Text = text;
            this.Glow = glow;
            this.Color = color;
        }
    }

    public delegate void STNodeOptionEventHandler(object sender, STNodeOptionEventArgs e);

    public class STNodeOptionEventArgs : EventArgs
    {
        private STNodeOption _TargetOption;
        /// <summary>
        /// The corresponding Option that triggered this event.
        /// </summary>
        public STNodeOption TargetOption {
            get { return _TargetOption; }
        }

        private ConnectionStatus _Status;
        /// <summary>
        /// Connection status between options.
        /// </summary>
        public ConnectionStatus Status {
            get { return _Status; }
            internal set { _Status = value; }
        }

        private bool _IsSponsor;
        /// <summary>
        /// Is it the initiator of this behavior?
        /// </summary>
        public bool IsSponsor {
            get { return _IsSponsor; }
        }

        public STNodeOptionEventArgs(bool isSponsor, STNodeOption opTarget, ConnectionStatus cr) {
            this._IsSponsor = isSponsor;
            this._TargetOption = opTarget;
            this._Status = cr;
        }
    }

    public delegate void STNodeEditorEventHandler(object sender, STNodeEditorEventArgs e);
    public delegate void STNodeEditorOptionEventHandler(object sender, STNodeEditorOptionEventArgs e);


    public class STNodeEditorEventArgs : EventArgs
    {
        private STNode _Node;

        public STNode Node {
            get { return _Node; }
        }

        public STNodeEditorEventArgs(STNode node) {
            this._Node = node;
        }
    }

    public class STNodeEditorOptionEventArgs : STNodeOptionEventArgs
    {

        private STNodeOption _CurrentOption;
        /// <summary>
        /// Option that triggers the event actively.
        /// </summary>
        public STNodeOption CurrentOption {
            get { return _CurrentOption; }
        }

        private bool _Continue = true;
        /// <summary>
        /// Whether to continue downward operation Used for Begin (Connecting/Disconnecting) whether to continue backward operation.
        /// </summary>
        public bool Continue {
            get { return _Continue; }
            set { _Continue = value; }
        }

        public STNodeEditorOptionEventArgs(STNodeOption opTarget, STNodeOption opCurrent, ConnectionStatus cr)
            : base(false, opTarget, cr) {
            this._CurrentOption = opCurrent;
        }
    }

    public struct NodeMovement
    {
        public STNode Node;
        public Point OldLocation;
        public Point NewLocation;
    }

    public class STNodesMovedEventArgs : EventArgs
    {
        public STNodesMovedEventArgs(NodeMovement[] movements)
        {
            mMovements = movements;
        }

        public NodeMovement[] Movements
        {
            get { return mMovements; }
        }

        private NodeMovement[] mMovements;
    }

    public delegate void STNodesMovedEventHandler(object sender, STNodesMovedEventArgs e);
}
