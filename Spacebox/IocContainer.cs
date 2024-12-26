using DryIoc;
using System;
using OpenTK.Windowing.Desktop;

using Spacebox.Game.Effects;
using Spacebox.Game.Generation;

namespace Spacebox
{

    public class IoCContainer
    {
        private static readonly Lazy<IContainer> _container = new(() =>
        {
            var container = new Container();
            RegisterServices(container);
            return container;
        });

        public static IContainer Instance => _container.Value;

        private static void RegisterServices(IContainer container)
        {

            container.Register<DropEffectManager>(Reuse.Singleton);
  
       
            container.Register<Chunk>(
                Reuse.Transient,
                setup: Setup.With(allowDisposableTransient: true));

            //container.Register<IGameWindow, GameWindow>(Reuse.Singleton);
        }
    }
}
