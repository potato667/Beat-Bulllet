using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework.Input;

namespace Beat_Bullet.Entities
{
    public partial class Player
    {
        public I2DInput MovementInput { get; set; }
        public IPressableInput BoostInput { get; set; }
        private void CustomInitialize()
        {
            
        }

        private void CustomActivity()
        {
                float movementSpeed = 125;
            if (MovementInput != null)
            {
                this.XVelocity = MovementInput.X * movementSpeed;
            }

            BulletCreationActivity();
        }

        private void BulletCreationActivity()
        {
            if (InputManager.Keyboard.KeyPushed(Keys.Space))
            {
                var newBulletInstance = Factories.BulletFactory.CreateNew();
                newBulletInstance.X = this.X;
                newBulletInstance.Y = this.Y;
            }
        }

        private void CustomDestroy()
        {


        }

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
    }
}
