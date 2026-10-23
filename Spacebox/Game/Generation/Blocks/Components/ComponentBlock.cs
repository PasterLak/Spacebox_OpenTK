using OpenTK.Mathematics;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks.Components;

public class ComponentBlock : InteractiveBlock, IEntityBlockListener, IExplosiveBlock
{
    private readonly BlockComponent[] _components;
    private SpaceEntity _entity;
    private Vector3i _cell;

    public ComponentBlock(BlockData blockData, BlockComponentDefinition[] definitions) : base(blockData)
    {
        Explosion = (blockData as IExplosiveBlockData)?.Explosion;
        _components = CreateComponents(definitions);

        for (int i = 0; i < _components.Length; i++)
            _components[i].Attach(this);

        for (int i = 0; i < _components.Length; i++)
        {
            var component = _components[i];
            if (component.ConsumesPower) ConsumesPower = true;
            if (component.ConsumesPower || component.GeneratesPower) UsesNetworkPower = true;
            if (component.UsePriority > 0 && (UseComponent == null || component.UsePriority > UseComponent.UsePriority)) UseComponent = component;
        }

        if (_components.Length > 0) OnDestroy += DispatchDestroyed;
    }

    public ExplosiveBlockData Explosion { get; }
    public bool ConsumesPower { get; }
    public bool UsesNetworkPower { get; }
    public SpaceEntity Entity => _entity;
    public Vector3i Cell => _cell;
    public bool IsEntityDisposed => _entity != null && _entity.IsDisposed;
    public bool IsLoading { get; internal set; }
    public int ComponentCount => _components.Length;
    protected BlockComponent UseComponent { get; }

    public BlockComponent GetComponent(int index) => _components[index];

    public bool TryGet<T>(out T component) where T : class
    {
        for (int i = 0; i < _components.Length; i++)
        {
            if (_components[i] is T match)
            {
                component = match;
                return true;
            }
        }

        component = null;
        return false;
    }

    public virtual void MarkStateDirty()
    {
    }

    public void OnAddedToEntity(Chunk chunk, Vector3i localCell)
    {
        this.chunk = chunk;
        _entity = chunk.SpaceEntity;
        _cell = localCell;

        for (int i = 0; i < _components.Length; i++)
            _components[i].OnAddedToEntity();
    }

    public void OnRemovedFromEntity()
    {
        _entity = null;

        for (int i = 0; i < _components.Length; i++)
            _components[i].OnRemovedFromEntity();
    }

    protected override void OnActiveStateChanged(bool isActive)
    {
        for (int i = 0; i < _components.Length; i++)
            _components[i].OnActiveStateChanged(isActive);
    }

    public override void TickElectric()
    {
        base.TickElectric();

        for (int i = 0; i < _components.Length; i++)
            _components[i].TickElectric();
    }

    public override void ConfigureAsMultiblockPart()
    {
        base.ConfigureAsMultiblockPart();

        for (int i = 0; i < _components.Length; i++)
            _components[i].ConfigureAsMultiblockPart();
    }

    protected override bool ShowsUseHint => UseComponent != null;

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        if (UseComponent == null) return;

        base.Use(player, ref hit);
        UseComponent.Use(player, ref hit);
    }

    private void DispatchDestroyed(HitInfo hit)
    {
        for (int i = 0; i < _components.Length; i++)
            _components[i].OnDestroyed(hit);
    }

    private static BlockComponent[] CreateComponents(BlockComponentDefinition[] definitions)
    {
        if (definitions == null || definitions.Length == 0) return Array.Empty<BlockComponent>();

        var components = new BlockComponent[definitions.Length];
        for (int i = 0; i < definitions.Length; i++)
            components[i] = definitions[i].Create();
        return components;
    }
}
