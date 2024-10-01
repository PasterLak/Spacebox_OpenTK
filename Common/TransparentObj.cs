using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spacebox.Common
{
    internal class TransparentObj
    {
        public  void Render()
        {
           /* GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Отрисовка непрозрачных объектов
            foreach (var obj in opaqueObjects)
            {
                RenderObject(obj);
            }

            // Включение смешивания перед отрисовкой прозрачных объектов
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Сортировка прозрачных объектов по удаленности (отдаленные сначала)
            var sortedTransparentObjects = transparentObjects
                .OrderByDescending(obj => Vector3.Distance(camera.Position, obj.Position))
                .ToList();

            // Отрисовка прозрачных объектов
            foreach (var obj in sortedTransparentObjects)
            {
                RenderObject(obj);
            }

            // Отключение смешивания после отрисовки
            GL.Disable(EnableCap.Blend);

            SceneManager.Instance.GameWindow.SwapBuffers();*/
        }

    }
}
