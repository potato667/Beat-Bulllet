#if ANDROID || IOS || DESKTOP_GL
#define REQUIRES_PRIMARY_THREAD_LOADING
#endif
#define SUPPORTS_GLUEVIEW_2
using Color = Microsoft.Xna.Framework.Color;
using System.Linq;
using FlatRedBall;
using System;
using System.Collections.Generic;
using System.Text;
namespace Beat_Bullet.Screens
{
    public partial class GameScreen : FlatRedBall.Screens.Screen
    {
        #if DEBUG
        static bool HasBeenLoadedWithGlobalContentManager = false;
        #endif
        protected static Microsoft.Xna.Framework.Media.Song TopDownBoyFinalSong;
        protected static Microsoft.Xna.Framework.Graphics.Texture2D BackGroundPiclol;
        protected static Microsoft.Xna.Framework.Graphics.Texture2D postglueoingdx;
        protected static Microsoft.Xna.Framework.Graphics.Texture2D postproccescinggluethinggamething;
        
        private Beat_Bullet.Entities.Beats BeatsInstance;
        private Beat_Bullet.Entities.Player PlayerInstance;
        private FlatRedBall.Sprite Background;
        private FlatRedBall.Sprite Foreground1;
        private FlatRedBall.Sprite Foreground2;
        private Beat_Bullet.Entities.Bullet BulletInstance;
        private FlatRedBall.Math.Collision.CollisionRelationship<Beat_Bullet.Entities.Bullet, Beat_Bullet.Entities.Beats> BulletInstanceVsBeatsInstance;
        private FlatRedBall.Math.Collision.CollisionRelationship<Beat_Bullet.Entities.Player, Beat_Bullet.Entities.Beats> PlayerInstanceVsBeatsInstance;
        private Beat_Bullet.Entities.ScoreHUD ScoreHUDInstance;
        public GameScreen () 
        	: base ("GameScreen")
        {
        }
        public override void Initialize (bool addToManagers) 
        {
            LoadStaticContent(ContentManagerName);
            BeatsInstance = new Beat_Bullet.Entities.Beats(ContentManagerName, false);
            BeatsInstance.Name = "BeatsInstance";
            BeatsInstance.CreationSource = "Glue";
            PlayerInstance = new Beat_Bullet.Entities.Player(ContentManagerName, false);
            PlayerInstance.Name = "PlayerInstance";
            PlayerInstance.CreationSource = "Glue";
            Background = new FlatRedBall.Sprite();
            Background.Name = "Background";
            Background.CreationSource = "Glue";
            Foreground1 = new FlatRedBall.Sprite();
            Foreground1.Name = "Foreground1";
            Foreground1.CreationSource = "Glue";
            Foreground2 = new FlatRedBall.Sprite();
            Foreground2.Name = "Foreground2";
            Foreground2.CreationSource = "Glue";
            BulletInstance = new Beat_Bullet.Entities.Bullet(ContentManagerName, false);
            BulletInstance.Name = "BulletInstance";
            BulletInstance.CreationSource = "Glue";
            ScoreHUDInstance = new Beat_Bullet.Entities.ScoreHUD(ContentManagerName, false);
            ScoreHUDInstance.Name = "ScoreHUDInstance";
            ScoreHUDInstance.CreationSource = "Glue";
                BulletInstanceVsBeatsInstance = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(BulletInstance, BeatsInstance);
    BulletInstanceVsBeatsInstance.Name = "BulletInstanceVsBeatsInstance";

                PlayerInstanceVsBeatsInstance = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(PlayerInstance, BeatsInstance);
    PlayerInstanceVsBeatsInstance.Name = "PlayerInstanceVsBeatsInstance";

            
            
            PostInitialize();
            base.Initialize(addToManagers);
            if (addToManagers)
            {
                AddToManagers();
            }
        }
        public override void AddToManagers () 
        {
            FlatRedBall.Audio.AudioManager.StopAndDisposeCurrentSongIfNameDiffers(TopDownBoyFinalSong.Name); FlatRedBall.Audio.AudioManager.PlaySong(TopDownBoyFinalSong, false, ContentManagerName == "Global");
            BeatsInstance.AddToManagers(mLayer);
            PlayerInstance.AddToManagers(mLayer);
            FlatRedBall.SpriteManager.AddSprite(Background);
            FlatRedBall.SpriteManager.AddToLayer(Foreground1, FlatRedBall.SpriteManager.TopLayer);
            FlatRedBall.SpriteManager.AddToLayer(Foreground2, FlatRedBall.SpriteManager.TopLayer);
            BulletInstance.AddToManagers(mLayer);
            ScoreHUDInstance.AddToManagers(mLayer);
            base.AddToManagers();
            AddToManagersBottomUp();
            BeforeCustomInitialize?.Invoke();
            CustomInitialize();
        }
        public override void Activity (bool firstTimeCalled) 
        {
            if (!IsPaused)
            {
                
                BeatsInstance.Activity();
                PlayerInstance.Activity();
                BulletInstance.Activity();
                ScoreHUDInstance.Activity();
            }
            else
            {
            }
            base.Activity(firstTimeCalled);
            if (!IsActivityFinished)
            {
                CustomActivity(firstTimeCalled);
            }
        }
        public override void Destroy () 
        {
            base.Destroy();
            FlatRedBall.Audio.AudioManager.StopSong();
            if (this.UnloadsContentManagerWhenDestroyed && ContentManagerName != "Global")
            {
                TopDownBoyFinalSong.Dispose();
            }
            TopDownBoyFinalSong = null;
            BackGroundPiclol = null;
            postglueoingdx = null;
            postproccescinggluethinggamething = null;
            
            if (BeatsInstance != null)
            {
                BeatsInstance.Destroy();
                BeatsInstance.Detach();
            }
            if (PlayerInstance != null)
            {
                PlayerInstance.Destroy();
                PlayerInstance.Detach();
            }
            if (Background != null)
            {
                FlatRedBall.SpriteManager.RemoveSprite(Background);
            }
            if (Foreground1 != null)
            {
                FlatRedBall.SpriteManager.RemoveSprite(Foreground1);
            }
            if (Foreground2 != null)
            {
                FlatRedBall.SpriteManager.RemoveSprite(Foreground2);
            }
            if (BulletInstance != null)
            {
                BulletInstance.Destroy();
                BulletInstance.Detach();
            }
            if (ScoreHUDInstance != null)
            {
                ScoreHUDInstance.Destroy();
                ScoreHUDInstance.Detach();
            }
            FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Clear();
            CustomDestroy();
        }
        public virtual void PostInitialize () 
        {
            bool oldShapeManagerSuppressAdd = FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue;
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = true;
            if (BeatsInstance.Parent == null)
            {
                BeatsInstance.X = -32f;
            }
            else
            {
                BeatsInstance.RelativeX = -32f;
            }
            if (BeatsInstance.Parent == null)
            {
                BeatsInstance.Y = 280f;
            }
            else
            {
                BeatsInstance.RelativeY = 280f;
            }
            if (PlayerInstance.Parent == null)
            {
                PlayerInstance.X = -32f;
            }
            else
            {
                PlayerInstance.RelativeX = -32f;
            }
            if (PlayerInstance.Parent == null)
            {
                PlayerInstance.Y = 280f;
            }
            else
            {
                PlayerInstance.RelativeY = 280f;
            }
            if (Background.Parent == null)
            {
                Background.CopyAbsoluteToRelative();
                Background.RelativeZ += -40;
                Background.AttachTo(FlatRedBall.Camera.Main, false);
            }
            Background.Texture = BackGroundPiclol;
            Background.TextureScale = 2f;
            if (Foreground1.Parent == null)
            {
                Foreground1.CopyAbsoluteToRelative();
                Foreground1.RelativeZ += -40;
                Foreground1.AttachTo(FlatRedBall.Camera.Main, false);
            }
            if (Foreground1.Parent == null)
            {
                Foreground1.Y = -8f;
            }
            else
            {
                Foreground1.RelativeY = -8f;
            }
            Foreground1.Texture = postproccescinggluethinggamething;
            Foreground1.TextureScale = 15f;
            Foreground1.Width = 10000f;
            Foreground1.ColorOperation = FlatRedBall.Graphics.ColorOperation.Texture;
            Foreground1.Alpha = 0.18f;
            if (Foreground2.Parent == null)
            {
                Foreground2.CopyAbsoluteToRelative();
                Foreground2.RelativeZ += -40;
                Foreground2.AttachTo(FlatRedBall.Camera.Main, false);
            }
            Foreground2.Texture = postglueoingdx;
            Foreground2.TextureScale = 1f;
            if (BulletInstance.Parent == null)
            {
                BulletInstance.X = -32f;
            }
            else
            {
                BulletInstance.RelativeX = -32f;
            }
            if (BulletInstance.Parent == null)
            {
                BulletInstance.Y = 280f;
            }
            else
            {
                BulletInstance.RelativeY = 280f;
            }
            if (ScoreHUDInstance.Parent == null)
            {
                ScoreHUDInstance.CopyAbsoluteToRelative();
                ScoreHUDInstance.RelativeZ += -40;
                ScoreHUDInstance.AttachTo(FlatRedBall.Camera.Main, false);
            }
            Microsoft.Xna.Framework.Media.MediaPlayer.IsRepeating = false;
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = oldShapeManagerSuppressAdd;
        }
        public virtual void AddToManagersBottomUp () 
        {
            CameraSetup.ResetCamera(SpriteManager.Camera);
            AssignCustomVariables(false);
        }
        public virtual void RemoveFromManagers () 
        {
            FlatRedBall.Audio.AudioManager.StopSong();
            BeatsInstance.RemoveFromManagers();
            PlayerInstance.RemoveFromManagers();
            if (Background != null)
            {
                FlatRedBall.SpriteManager.RemoveSpriteOneWay(Background);
            }
            if (Foreground1 != null)
            {
                FlatRedBall.SpriteManager.RemoveSpriteOneWay(Foreground1);
            }
            if (Foreground2 != null)
            {
                FlatRedBall.SpriteManager.RemoveSpriteOneWay(Foreground2);
            }
            BulletInstance.RemoveFromManagers();
            ScoreHUDInstance.RemoveFromManagers();
        }
        public virtual void AssignCustomVariables (bool callOnContainedElements) 
        {
            if (callOnContainedElements)
            {
                BeatsInstance.AssignCustomVariables(true);
                PlayerInstance.AssignCustomVariables(true);
                BulletInstance.AssignCustomVariables(true);
                ScoreHUDInstance.AssignCustomVariables(true);
            }
            if (BeatsInstance.Parent == null)
            {
                BeatsInstance.X = -32f;
            }
            else
            {
                BeatsInstance.RelativeX = -32f;
            }
            if (BeatsInstance.Parent == null)
            {
                BeatsInstance.Y = 280f;
            }
            else
            {
                BeatsInstance.RelativeY = 280f;
            }
            if (PlayerInstance.Parent == null)
            {
                PlayerInstance.X = -32f;
            }
            else
            {
                PlayerInstance.RelativeX = -32f;
            }
            if (PlayerInstance.Parent == null)
            {
                PlayerInstance.Y = 280f;
            }
            else
            {
                PlayerInstance.RelativeY = 280f;
            }
            Background.Texture = BackGroundPiclol;
            Background.TextureScale = 2f;
            if (Foreground1.Parent == null)
            {
                Foreground1.Y = -8f;
            }
            else
            {
                Foreground1.RelativeY = -8f;
            }
            Foreground1.Texture = postproccescinggluethinggamething;
            Foreground1.TextureScale = 15f;
            Foreground1.Width = 10000f;
            Foreground1.ColorOperation = FlatRedBall.Graphics.ColorOperation.Texture;
            Foreground1.Alpha = 0.18f;
            Foreground2.Texture = postglueoingdx;
            Foreground2.TextureScale = 1f;
            if (BulletInstance.Parent == null)
            {
                BulletInstance.X = -32f;
            }
            else
            {
                BulletInstance.RelativeX = -32f;
            }
            if (BulletInstance.Parent == null)
            {
                BulletInstance.Y = 280f;
            }
            else
            {
                BulletInstance.RelativeY = 280f;
            }
        }
        public virtual void ConvertToManuallyUpdated () 
        {
            BeatsInstance.ConvertToManuallyUpdated();
            PlayerInstance.ConvertToManuallyUpdated();
            FlatRedBall.SpriteManager.ConvertToManuallyUpdated(Background);
            FlatRedBall.SpriteManager.ConvertToManuallyUpdated(Foreground1);
            FlatRedBall.SpriteManager.ConvertToManuallyUpdated(Foreground2);
            BulletInstance.ConvertToManuallyUpdated();
            ScoreHUDInstance.ConvertToManuallyUpdated();
        }
        public static void LoadStaticContent (string contentManagerName) 
        {
            if (string.IsNullOrEmpty(contentManagerName))
            {
                throw new System.ArgumentException("contentManagerName cannot be empty or null");
            }
            #if DEBUG
            if (contentManagerName == FlatRedBall.FlatRedBallServices.GlobalContentManager)
            {
                HasBeenLoadedWithGlobalContentManager = true;
            }
            else if (HasBeenLoadedWithGlobalContentManager)
            {
                throw new System.Exception("This type has been loaded with a Global content manager, then loaded with a non-global.  This can lead to a lot of bugs");
            }
            #endif
            TopDownBoyFinalSong = FlatRedBall.FlatRedBallServices.Load<Microsoft.Xna.Framework.Media.Song>(@"content/screens/gamescreen/topdownboyfinalsong", contentManagerName);
            BackGroundPiclol = FlatRedBall.FlatRedBallServices.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(@"content/screens/gamescreen/backgroundpiclol.png", contentManagerName);
            postglueoingdx = FlatRedBall.FlatRedBallServices.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(@"content/screens/gamescreen/postglueoingdx.png", contentManagerName);
            postproccescinggluethinggamething = FlatRedBall.FlatRedBallServices.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(@"content/screens/gamescreen/postproccescinggluethinggamething.png", contentManagerName);
            Beat_Bullet.Entities.Beats.LoadStaticContent(contentManagerName);
            Beat_Bullet.Entities.Player.LoadStaticContent(contentManagerName);
            Beat_Bullet.Entities.Bullet.LoadStaticContent(contentManagerName);
            Beat_Bullet.Entities.ScoreHUD.LoadStaticContent(contentManagerName);
            CustomLoadStaticContent(contentManagerName);
        }
        public override void PauseThisScreen () 
        {
            StateInterpolationPlugin.TweenerManager.Self.Pause();
            base.PauseThisScreen();
        }
        public override void UnpauseThisScreen () 
        {
            StateInterpolationPlugin.TweenerManager.Self.Unpause();
            base.UnpauseThisScreen();
        }
        [System.Obsolete("Use GetFile instead")]
        public static object GetStaticMember (string memberName) 
        {
            switch(memberName)
            {
                case  "TopDownBoyFinalSong":
                    return TopDownBoyFinalSong;
                case  "BackGroundPiclol":
                    return BackGroundPiclol;
                case  "postglueoingdx":
                    return postglueoingdx;
                case  "postproccescinggluethinggamething":
                    return postproccescinggluethinggamething;
            }
            return null;
        }
        public static object GetFile (string memberName) 
        {
            switch(memberName)
            {
                case  "TopDownBoyFinalSong":
                    return TopDownBoyFinalSong;
                case  "BackGroundPiclol":
                    return BackGroundPiclol;
                case  "postglueoingdx":
                    return postglueoingdx;
                case  "postproccescinggluethinggamething":
                    return postproccescinggluethinggamething;
            }
            return null;
        }
        object GetMember (string memberName) 
        {
            switch(memberName)
            {
                case  "TopDownBoyFinalSong":
                    return TopDownBoyFinalSong;
                case  "BackGroundPiclol":
                    return BackGroundPiclol;
                case  "postglueoingdx":
                    return postglueoingdx;
                case  "postproccescinggluethinggamething":
                    return postproccescinggluethinggamething;
            }
            return null;
        }
    }
}
