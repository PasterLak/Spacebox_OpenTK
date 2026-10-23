namespace Spacebox.Game.GUI.Menu;

public interface IOptionRowVisitor
{
    void Visit(SectionRow row);
    void Visit(SliderRow row);
    void Visit(CheckRow row);
    void Visit(ChoiceRow row);
    void Visit(ButtonRow row);
    void Visit(TextRow row);
}
