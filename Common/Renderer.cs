using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Renderer
    {
        private List<INotTransparent> _opaqueDrawables = new List<INotTransparent>();
        private List<ITransparent> _transparentDrawables = new List<ITransparent>();

        public void AddDrawable(IDrawable drawable)
        {
            if (drawable is ITransparent transparent)
            {
                _transparentDrawables.Add(transparent);
            }
            if (drawable is INotTransparent opaque)
            {
                _opaqueDrawables.Add(opaque);
            }
        }

        public void RemoveDrawable(IDrawable drawable)
        {
            if (drawable is ITransparent transparent)
            {
                _transparentDrawables.Remove(transparent);
            }
            else if (drawable is INotTransparent opaque)
            {
                _opaqueDrawables.Remove(opaque);
            }
        }

        public void RenderAll(Camera camera)
        {
            // Отрисовка непрозрачных объектов
            foreach (var drawable in _opaqueDrawables)
            {
                drawable.Draw(camera);
            }

            // Включение смешивания для прозрачных объектов
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.DepthMask(false); // Отключаем запись в буфер глубины

            // Сортировка прозрачных объектов по убыванию расстояния от камеры
            var sortedTransparent = _transparentDrawables
                .OrderByDescending(d => Vector3.Distance(camera.Position, ((Transform)d).Position))
                .ToList();

            foreach (var transparent in sortedTransparent)
            {
                transparent.DrawTransparent(camera);
            }

            GL.DepthMask(true); // Включаем запись в буфер глубины
            GL.Disable(EnableCap.Blend);
        }
    }
}