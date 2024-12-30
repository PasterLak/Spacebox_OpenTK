namespace Spacebox.Game.Player;

public class InteractionHandler
{
    private InteractionMode _interactionMode;
    private readonly HashSet<Type> _allowedInteractions;

    public InteractionHandler()
    {
        _interactionMode = new InteractionDefault();
        _allowedInteractions = new HashSet<Type>()
        {
            typeof(InteractionDefault)
        };
    }

    public InteractionHandler(InteractionMode defaultMode, HashSet<Type> allowedInteractions)
    {
        _interactionMode = defaultMode;
        _allowedInteractions = allowedInteractions;

        _allowedInteractions.Add(typeof(InteractionDefault));
    }

    public void SetInteraction(InteractionMode iteration)
    {
        if (!_allowedInteractions.Contains(iteration.GetType())) return;

        _interactionMode = iteration;
    }

    public void Update(Astronaut player)
    {
        _interactionMode.Update(player);
    }
}