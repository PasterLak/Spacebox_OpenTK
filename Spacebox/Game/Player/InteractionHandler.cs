using Spacebox.Common;

namespace Spacebox.Game.Player;

public class InteractionHandler
{
    private InteractionMode _interaction;
    public InteractionMode Interaction {
        get => _interaction;
        set { SetInteraction(value); }
    }
    private readonly HashSet<Type> _allowedInteractions;

    public InteractionHandler()
    {
        _allowedInteractions = new HashSet<Type>()
        {
            typeof(InteractionDefault)
        };
        
        SetInteraction(new InteractionDefault());
    }

    public InteractionHandler(InteractionMode defaultMode, HashSet<Type> allowedInteractions)
    {
        _allowedInteractions = allowedInteractions;

        _allowedInteractions.Add(typeof(InteractionDefault));
        SetInteraction(defaultMode);
    }

    public void SetInteraction(InteractionMode interaction)
    {
        if(_interaction != null && _interaction.GetType() == interaction.GetType()) return;
        if (!_allowedInteractions.Contains(interaction.GetType()))
        {
            Debug.Error("[Interactionhandler] Invalid interaction type or this interaction is not allowed: " + interaction.GetType().Name);
            return;
        }
        
        if(_interaction != null) Interaction.OnDisable();

        interaction.OnEnable();
        _interaction = interaction;
    }

    public InteractionMode GetInteraction()
    {
        return Interaction;
    }

    public void Update(Astronaut player)
    {
        Interaction.Update(player);
    }
}