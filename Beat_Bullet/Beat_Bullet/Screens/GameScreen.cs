using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Localization;
using Microsoft.Xna.Framework;
using Beat_Bullet.Entities;
using System.Threading;

namespace Beat_Bullet.Screens
{
    public partial class GameScreen
    {

        void CustomInitialize()
        {


        }

        void CustomActivity(bool firstTimeCalled)
        {
            //This line of code makes the mouse cursor visible in game.
            FlatRedBallServices.Game.IsMouseVisible = true;

            //This code makes the camera follow the player.
            float movementCoefficient = 1;
            Vector3 velocityToSet = PlayerInstance.Position - Camera.Main.Position;
            velocityToSet.Z = 0;
            Camera.Main.Velocity = movementCoefficient * velocityToSet;

            // This code make the player move
            _ = Camera.Main.Position - PlayerInstance.Position;
            velocityToSet.X = 200;
            velocityToSet.Y = 0;
            PlayerInstance.Velocity = movementCoefficient * velocityToSet;

            // These are references for functions
            BeatsInstanceVsBulletInstanceCollisionOccurred(BulletInstance, BeatsInstance);

            PlayerInstanceVsBeatInstanceCollisionOccurred(PlayerInstance, BeatsInstance);
        }

        void CustomDestroy()
        {


        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

        void BeatsInstanceVsBulletInstanceCollisionOccurred(Entities.Bullet first, Entities.Beats second)
        {
            ScoreHUDInstance.Score1++;
        }

        void PlayerInstanceVsBeatInstanceCollisionOccurred(Entities.Player first, Entities.Beats second)
        {
            float framesPerSecond = 90 / TimeManager.SecondDifference;

            float FPS = 100 / TimeManager.SecondDifference;

            if (framesPerSecond <= FPS)
            {

            }

            else if (framesPerSecond >= FPS) 
            {
                RestartScreen();
            }
        }
    }
}
