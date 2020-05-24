﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Box2DX.Common;
using Box2DX.Dynamics;
using Box2DX.Collision;

using Color = Microsoft.Xna.Framework.Color;

using Asteroid.src.utils;
using Asteroid.src.physics.bodies;



namespace Asteroid.src.render
{
    class PolygonRenderer : IRenderer
    {
        Color Color { get; set; }
        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;
        VertexPositionColor[] vertices;
        ushort[] vertIndexes;

        public PolygonRenderer(Color color, int vertCount, GraphicsDevice graphicsDevice)
        {
            Color = color;
            // буфер для вершин полигона
            vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor),
             vertCount, BufferUsage.None);
            vertices = new VertexPositionColor[vertCount];
            vertIndexes = new ushort[vertCount * 2];
            // буфер для индексов чтобы сделать линии соеденяющимися
            indexBuffer = new IndexBuffer(graphicsDevice, typeof(ushort), vertIndexes.Length, BufferUsage.WriteOnly);
            // заполняю индексы для ребер(соедеденяю каждую вершину)
            for(ushort i = 0, indexOffset = 0; i < vertCount; i++, indexOffset += 2)
            {
                vertIndexes[indexOffset] = i;
                vertIndexes[indexOffset + 1] = (ushort)((i + 1) % vertCount);
            }
            indexBuffer.SetData(vertIndexes);
        }
        // TODO IDEA: 
        // сделать так, чтобы цвет вершин по краям карты тускнел
        // и так же, чтобы края карты отталкивали игрока к центру
        public void Render(IBody body, SpriteBatch spriteBatch)
        {
            var boxBody = body as BoxBody;
            var shape = (PolygonShape)body.RealBody.GetShapeList();

            var worldMatrix = Matrix.CreateWorld(new Vector3(0f, 0f, 0f), new Vector3(0, 0, -1), Vector3.Up);

            // перевод box2d вершин в экранные

            for (int i = 0; i < shape.VertexCount; i++)
            {
                Vec2 vec = body.RealBody.GetWorldPoint(shape.GetVertices()[i]);
                vertices[i].Position = new Vector3(vec.X, vec.Y, 0);
                vertices[i].Color = Color;// можно рандомить, шейдер будет интерполировать
            }

            vertexBuffer.SetData(vertices);

            Camera.CurrentEffect.World = worldMatrix;
            
            spriteBatch.GraphicsDevice.SetVertexBuffer(vertexBuffer);
            spriteBatch.GraphicsDevice.Indices = indexBuffer;


            foreach (EffectPass pass in Camera.CurrentEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                spriteBatch.GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.LineList, 0, 0, vertIndexes.Length);
            }
        }
    }
}
