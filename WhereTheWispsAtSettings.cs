using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using SharpDX;

namespace WhereTheWispsAt;

public class WhereTheWispsAtSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);
    public ToggleNode DrawMap { get; set; } = new ToggleNode(true);
    public ColorNode BlueWisp { get; set; } = new ColorNode(System.Drawing.Color.SkyBlue);
    public ColorNode Rituals { get; set; } = new ColorNode(System.Drawing.Color.Red);
    public ColorNode Dealer { get; set; } = new ColorNode(System.Drawing.Color.HotPink);
    public ColorNode Breach { get; set; } = new ColorNode(System.Drawing.Color.HotPink);
    public ToggleNode IgnoreFullscreenPanels { get; set; } = new ToggleNode(false);
    public ToggleNode IgnoreLargePanels { get; set; } = new ToggleNode(false);
    public TextNode CustomMetadata { get; set; } = new TextNode();
    public RangeNode<int> BreachX { get; set; } = new RangeNode<int>(200,00,1920);
    public RangeNode<int> BreachY { get; set; } = new RangeNode<int>(200,00,1080);
    public RangeNode<int> BreachRadius { get; set; } = new RangeNode<int>(500,450,600);
    public RangeNode<int> BreachK { get; set; } = new RangeNode<int>(4000,1000,6000);
    public RangeNode<int> BreachTfast { get; set; } = new RangeNode<int>(1200,1000,1500);
    public ToggleNode DrawBreachExpand { get; set; } = new ToggleNode(false);
    public ButtonNode PressMe { get; set; } = new();
}