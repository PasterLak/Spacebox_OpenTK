using Engine;
using OpenTK.Mathematics;


namespace Spacebox.Game.GUI
{
    public class TagJSON
    {
        public string Text { get; set; } = "TagJSON_Uninitialized";
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public bool Visible { get; set; } = true;
        public Color4Byte Color { get; set; } = Color4Byte.Pink;

        public TagJSON() { }

        public TagJSON(Tag tag)
        {
            Text = tag.Text;
            X = tag.WorldPosition.X;
            Y = tag.WorldPosition.Y;
            Z = tag.WorldPosition.Z;
            Visible = tag.Visible;
            Color = new Color4Byte(tag.Color);
        }

        public Tag CreateTag()
        {
            var worldPos = new Vector3(X, Y, Z);
            var color = Color.ToColor4();

            var newTag = TagManager.Instance.CreateTag(Text, worldPos, color, isStatic: true);
            newTag.Visible = Visible;

            return newTag;
        }
    }
}
