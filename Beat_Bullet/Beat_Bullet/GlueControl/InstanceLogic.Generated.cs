﻿#define IncludeSetVariable
#define SupportsEditMode

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Math;
using FlatRedBall.Utilities;

using FlatRedBall.Screens;
using FlatRedBall.Graphics;

using GlueControl.Models;
using System.Collections;
using GlueControl.Runtime;
using GlueControl.Dtos;
using GlueControl.Editing;

namespace GlueControl
{
    public class InstanceLogic
    {
        #region Objects added at runtime 
        public List<ShapeCollection> ShapeCollectionsAddedAtRuntime = new List<ShapeCollection>();

        public ShapeCollection ShapesAddedAtRuntime = new ShapeCollection();

        public FlatRedBall.Math.PositionedObjectList<Sprite> SpritesAddedAtRuntime = new FlatRedBall.Math.PositionedObjectList<Sprite>();
        public FlatRedBall.Math.PositionedObjectList<Text> TextsAddedAtRuntime = new FlatRedBall.Math.PositionedObjectList<Text>();

        public List<IDestroyable> DestroyablesAddedAtRuntime = new List<IDestroyable>();

        // Do we want to support entire categories at runtime? For now just states, but we'll have to review this at some point
        // if we want to allow entire categories added at runtime. The key is the game type (GameNamespace.Entities.EntityName)
        public Dictionary<string, List<StateSaveCategory>> StatesAddedAtRuntime = new Dictionary<string, List<StateSaveCategory>>();

        public Dictionary<string, List<CustomVariable>> CustomVariablesAddedAtRuntime = new Dictionary<string, List<CustomVariable>>();


        public List<IList> ListsAddedAtRuntime = new List<IList>();

#if HasGum
        public List<Gum.Wireframe.GraphicalUiElement> GumObjectsAddedAtRuntime = new List<Gum.Wireframe.GraphicalUiElement>();
        public List<GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper> GumWrappersAddedAtRuntime = new List<GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper>();
#endif

        #endregion

        #region Fields/Properties

        static InstanceLogic self;
        public static InstanceLogic Self
        {
            get
            {
                if (self == null)
                {
                    self = new InstanceLogic();
                }
                return self;
            }
        }



        /// <summary>
        /// A dictionary of custom elements where the key is the full name of the type that
        /// would exist if the code were generated (such as "ProjectNamespace.Entities.MyEntity")
        /// </summary>
        public Dictionary<string, GlueElement> CustomGlueElements = new Dictionary<string, GlueElement>();


        // this is to prevent multiple objects from having the same name in the same frame:
        static long NewIndex = 0;

        #endregion

        #region Create Instance from Glue

        public object HandleCreateInstanceCommandFromGlue(Dtos.AddObjectDto dto, int currentAddObjectIndex, PositionedObject forcedItem = null)
        {
            //var glueName = dto.ElementName;
            // this comes in as the game name not glue name
            var elementGameType = dto.ElementNameGame; // CommandReceiver.GlueToGameElementName(glueName);
            var ownerType = this.GetType().Assembly.GetType(elementGameType);
            GlueElement ownerElement = null;
            if (CustomGlueElements.ContainsKey(elementGameType))
            {
                ownerElement = CustomGlueElements[elementGameType];
            }

            var addedToEntity =
                (ownerType != null && typeof(PositionedObject).IsAssignableFrom(ownerType))
                ||
                ownerElement != null && ownerElement is EntitySave;

            if (addedToEntity)
            {
                if (forcedItem != null)
                {
                    if (CommandReceiver.DoTypesMatch(forcedItem, elementGameType))
                    {
                        HandleCreateInstanceCommandFromGlueInner(dto, currentAddObjectIndex, forcedItem);
                    }
                }
                else
                {
                    // need to loop through every object and see if it is an instance of the entity type, and if so, add this object to it
                    for (int i = 0; i < SpriteManager.ManagedPositionedObjects.Count; i++)
                    {
                        var item = SpriteManager.ManagedPositionedObjects[i];
                        if (CommandReceiver.DoTypesMatch(item, elementGameType))
                        {
                            HandleCreateInstanceCommandFromGlueInner(dto, currentAddObjectIndex, item);
                        }
                    }
                }
            }
            else if (forcedItem == null &&
                (ScreenManager.CurrentScreen.GetType().FullName == elementGameType || ownerType?.IsAssignableFrom(ScreenManager.CurrentScreen.GetType()) == true))
            {
                // it's added to the base screen, so just add it to null
                HandleCreateInstanceCommandFromGlueInner(dto, currentAddObjectIndex, null);
            }
            return dto;
        }

        private object HandleCreateInstanceCommandFromGlueInner(Models.NamedObjectSave deserialized, int currentAddObjectIndex, PositionedObject owner)
        {
            // The owner is the
            // PositionedObject which
            // owns the newly-created instance
            // from the NamedObjectSave. Note that
            // if the owner is a DynamicEntity, it will
            // automatically remove any attached objects; 
            // however, if it is not, the objects still need
            // to be removed by the Glue control system, so we 
            // are going to add them to the ShapesAddedAtRuntime

            PositionedObject newPositionedObject = null;
            object newObject = null;

            if (deserialized.SourceType == GlueControl.Models.SourceType.Entity)
            {
                newPositionedObject = CreateEntity(deserialized);

                var sourceClassTypeGame = CommandReceiver.GlueToGameElementName(deserialized.SourceClassType);

                for (int i = 0; i < currentAddObjectIndex; i++)
                {
                    var dto = CommandReceiver.GlobalGlueToGameCommands[i];
                    if (dto is Dtos.AddObjectDto addObjectDtoRerun)
                    {
                        HandleCreateInstanceCommandFromGlue(addObjectDtoRerun, currentAddObjectIndex, newPositionedObject);
                    }
                    else if (dto is Dtos.GlueVariableSetData glueVariableSetDataRerun)
                    {
                        GlueControl.Editing.VariableAssignmentLogic.SetVariable(glueVariableSetDataRerun, newPositionedObject);
                    }
                    else if (dto is RemoveObjectDto removeObjectDtoRerun)
                    {
                        HandleDeleteInstanceCommandFromGlue(removeObjectDtoRerun, newPositionedObject);
                    }
                }
            }
            else if (deserialized.SourceType == GlueControl.Models.SourceType.FlatRedBallType &&
                deserialized.IsCollisionRelationship())
            {
                newObject = TryCreateCollisionRelationship(deserialized);
            }
            else if (deserialized.SourceType == GlueControl.Models.SourceType.FlatRedBallType)
            {
                switch (deserialized.SourceClassType)
                {
                    case "FlatRedBall.Math.Geometry.AxisAlignedRectangle":
                    case "AxisAlignedRectangle":
                        {
                            var aaRect = new FlatRedBall.Math.Geometry.AxisAlignedRectangle();
                            if (deserialized.AddToManagers)
                            {
                                ShapeManager.AddAxisAlignedRectangle(aaRect);
                                ShapesAddedAtRuntime.Add(aaRect);
                            }
                            if (owner is ICollidable asCollidable && deserialized.IncludeInICollidable)
                            {
                                asCollidable.Collision.Add(aaRect);
                            }
                            newPositionedObject = aaRect;
                        }

                        break;
                    case "FlatRedBall.Math.Geometry.Circle":
                    case "Circle":
                        {
                            var circle = new FlatRedBall.Math.Geometry.Circle();
                            if (deserialized.AddToManagers)
                            {
                                ShapeManager.AddCircle(circle);
                                ShapesAddedAtRuntime.Add(circle);
                            }
                            if (owner is ICollidable asCollidable && deserialized.IncludeInICollidable)
                            {
                                asCollidable.Collision.Add(circle);
                            }
                            newPositionedObject = circle;
                        }
                        break;
                    case "FlatRedBall.Math.Geometry.Polygon":
                    case "Polygon":
                        {
                            var polygon = new FlatRedBall.Math.Geometry.Polygon();
                            if (deserialized.AddToManagers)
                            {
                                ShapeManager.AddPolygon(polygon);
                                ShapesAddedAtRuntime.Add(polygon);
                            }
                            if (owner is ICollidable asCollidable && deserialized.IncludeInICollidable)
                            {
                                asCollidable.Collision.Add(polygon);
                            }
                            newPositionedObject = polygon;
                        }
                        break;
                    case "FlatRedBall.Sprite":
                    case "Sprite":
                        var sprite = new FlatRedBall.Sprite();
                        if (deserialized.AddToManagers)
                        {
                            SpriteManager.AddSprite(sprite);
                            SpritesAddedAtRuntime.Add(sprite);
                        }
                        newPositionedObject = sprite;

                        break;
                    case "Text":
                    case "FlatRedBall.Graphics.Text":
                        var text = new FlatRedBall.Graphics.Text();
                        text.Font = TextManager.DefaultFont;
                        text.SetPixelPerfectScale(Camera.Main);
                        if (deserialized.AddToManagers)
                        {
                            TextManager.AddText(text);
                            TextsAddedAtRuntime.Add(text);
                        }
                        newPositionedObject = text;
                        break;
                    case "FlatRedBall.Math.Geometry.ShapeCollection":
                    case "ShapeCollection":
                        var shapeCollection = new ShapeCollection();
                        ShapeCollectionsAddedAtRuntime.Add(shapeCollection);
                        newObject = shapeCollection;
                        break;
                    case "FlatRedBall.Math.PositionedObjectList<T>":
                        newObject = CreatePositionedObjectList(deserialized);
                        break;
                }

                if (newObject == null)
                {
                    newObject = TryCreateGumObject(deserialized, owner);
                }
            }
            if (newPositionedObject != null)
            {
                newObject = newPositionedObject;

                if (owner != null)
                {
                    newPositionedObject.AttachTo(owner);
                }
            }
            if (newObject != null)
            {
                AssignVariablesOnNewlyCreatedObject(deserialized, newObject);
            }

            return newObject;
        }

        private object TryCreateGumObject(NamedObjectSave deserialized, PositionedObject owner)
        {
#if HasGum
            var type = this.GetType().Assembly.GetType(deserialized.SourceClassType);
            var isGum = type != null && typeof(Gum.Wireframe.GraphicalUiElement).IsAssignableFrom(type);

            if (isGum)
            {
                var oldLayoutSuspended = global::Gum.Wireframe.GraphicalUiElement.IsAllLayoutSuspended;
                global::Gum.Wireframe.GraphicalUiElement.IsAllLayoutSuspended = true;
                var constructor = type.GetConstructor(new Type[] { typeof(bool), typeof(bool) });
                var newGumObjectInstance = 
                    constructor.Invoke(new object[] { true, true }) as Gum.Wireframe.GraphicalUiElement;

                global::Gum.Wireframe.GraphicalUiElement.IsAllLayoutSuspended = oldLayoutSuspended;
                newGumObjectInstance.UpdateFontRecursive();
                newGumObjectInstance.UpdateLayout();

                // eventually support layered, but not for now.....
                newGumObjectInstance.AddToManagers(RenderingLibrary.SystemManagers.Default, null);

                if (owner != null)
                {
                    var wrapperForAttachment = new GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper(owner, newGumObjectInstance);
                    FlatRedBall.SpriteManager.AddPositionedObject(wrapperForAttachment);
                    wrapperForAttachment.Name = deserialized.InstanceName;
                    //gumAttachmentWrappers.Add(wrapperForAttachment);
                    GumWrappersAddedAtRuntime.Add(wrapperForAttachment);
                }
                GumObjectsAddedAtRuntime.Add(newGumObjectInstance);

                return newGumObjectInstance;
            }
#endif
            return null;
        }

        private Object CreatePositionedObjectList(Models.NamedObjectSave namedObject)
        {
            var sourceClassGenericType = namedObject.SourceClassGenericType;

            var gameTypeName =
                CommandReceiver.GlueToGameElementName(sourceClassGenericType);

            var type = this.GetType().Assembly.GetType(gameTypeName);

            object newList = null;

            if (type == null)
            {
                // see if it's contained in the list of dynamic entities

                var isDynamicEntity = CustomGlueElements.ContainsKey(gameTypeName);
                if (isDynamicEntity)
                {
                    var list = new PositionedObjectList<DynamicEntity>();
                    ListsAddedAtRuntime.Add(list);
                    newList = list;
                }
                else
                {
                    var list = new PositionedObjectList<PositionedObject>();
                    ListsAddedAtRuntime.Add(list);
                    newList = list;
                }
            }
            else
            {
                var poList = typeof(PositionedObjectList<>).MakeGenericType(type);
                var list = poList.GetConstructor(new Type[0]).Invoke(new object[0]) as IList;
                ListsAddedAtRuntime.Add(list);
                newList = list;
            }
            return newList;
        }

        private object TryCreateCollisionRelationship(Models.NamedObjectSave deserialized)
        {
            var type = Editing.VariableAssignmentLogic.GetDesiredRelationshipType(deserialized, out object firstObject, out object secondObject);
            if (type == null)
            {
                return null;
            }
            else
            {
                object toReturn = null;
                var constructor = type.GetConstructors().FirstOrDefault();
                if (constructor != null)
                {
                    List<object> parameters = new List<object>();
                    if (firstObject != null)
                    {
                        parameters.Add(firstObject);
                    }
                    if (secondObject != null)
                    {
                        parameters.Add(secondObject);
                    }
                    var collisionRelationship =
                        constructor.Invoke(parameters.ToArray()) as FlatRedBall.Math.Collision.CollisionRelationship;
                    collisionRelationship.Partitions = FlatRedBall.Math.Collision.CollisionManager.Self.Partitions;
                    toReturn = collisionRelationship;
                    FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Add(collisionRelationship);
                }
                return toReturn;
            }
        }


        public PositionedObject CreateEntity(Models.NamedObjectSave deserialized)
        {
            var entityNameGlue = deserialized.SourceClassType;
            return CreateEntity(CommandReceiver.GlueToGameElementName(entityNameGlue));
        }

        public PositionedObject CreateEntity(string entityNameGameType)
        {
            var containsKey =
                CustomGlueElements.ContainsKey(entityNameGameType);
            if (!containsKey && !string.IsNullOrWhiteSpace(entityNameGameType) && entityNameGameType.Contains('.') == false)
            {
                // It may not be qualified, which means it is coming from content that doesn't qualify - like Tiled
                entityNameGameType = CustomGlueElements.Keys.FirstOrDefault(item => item.Split('.').Last() == entityNameGameType);
                // Now that we've qualified, try again
                if (!string.IsNullOrWhiteSpace(entityNameGameType))
                {
                    containsKey =
                        CustomGlueElements.ContainsKey(entityNameGameType);
                }
            }

            // This function may be given a qualified name like MyGame.Entities.MyEntity (if from Glue) 
            // or an unqualified name like MyEntity (if from Tiled). If from Tiled, then this code attempts
            // to fully qualify the entity name. This attempt to qualify may make the name null, so we need to
            // check and tolerate null.
            if (string.IsNullOrWhiteSpace(entityNameGameType))
            {
                return null;
            }
            else if (containsKey)
            {
                var dynamicEntityInstance = new Runtime.DynamicEntity();
                dynamicEntityInstance.EditModeType = entityNameGameType;
                SpriteManager.AddPositionedObject(dynamicEntityInstance);

                DestroyablesAddedAtRuntime.Add(dynamicEntityInstance);

                return dynamicEntityInstance;
            }
            else
            {
                PositionedObject newPositionedObject;
                var factory = FlatRedBall.TileEntities.TileEntityInstantiator.GetFactory(entityNameGameType);
                if (factory != null)
                {
                    newPositionedObject = factory?.CreateNew() as FlatRedBall.PositionedObject;
                }
                else
                {
                    // just instantiate it using reflection?
                    newPositionedObject = this.GetType().Assembly.CreateInstance(entityNameGameType)
                         as PositionedObject;
                    //newPositionedObject = ownerType.GetConstructor(new System.Type[0]).Invoke(new object[0]);
                }
                if (newPositionedObject != null && newPositionedObject is IDestroyable asDestroyable)
                {
                    DestroyablesAddedAtRuntime.Add(asDestroyable);
                }
                return newPositionedObject;
            }
        }

        private void AssignVariablesOnNewlyCreatedObject(Models.NamedObjectSave deserialized, object newObject)
        {
            if (newObject is FlatRedBall.Utilities.INameable asNameable)
            {
                asNameable.Name = deserialized.InstanceName;
            }
            if (newObject is PositionedObject asPositionedObject)
            {
                if (ScreenManager.IsInEditMode)
                {
                    asPositionedObject.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                    asPositionedObject.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
                }
                asPositionedObject.CreationSource = "Glue"; // Glue did make this, so do this so the game can select it
            }

            foreach (var instruction in deserialized.InstructionSaves)
            {
                AssignVariable(newObject, instruction);
            }
        }

        #endregion

        #region Delete Instance from Glue

        public RemoveObjectDtoResponse HandleDeleteInstanceCommandFromGlue(RemoveObjectDto removeObjectDto, PositionedObject forcedItem = null)
        {
            RemoveObjectDtoResponse response = new RemoveObjectDtoResponse();
            response.DidScreenMatch = false;
            response.WasObjectRemoved = false;

            var elementGameType = CommandReceiver.GlueToGameElementName(removeObjectDto.ElementNameGlue);
            var ownerType = this.GetType().Assembly.GetType(elementGameType);
            GlueElement ownerElement = null;
            if (CustomGlueElements.ContainsKey(elementGameType))
            {
                ownerElement = CustomGlueElements[elementGameType];
            }

            var removedFromEntity =
                (ownerType != null && typeof(PositionedObject).IsAssignableFrom(ownerType))
                ||
                ownerElement != null && ownerElement is EntitySave;

            if (removedFromEntity)
            {
                if (forcedItem != null)
                {
                    if (CommandReceiver.DoTypesMatch(forcedItem, elementGameType))
                    {
                        var objectToDelete = forcedItem.Children.FindByName(removeObjectDto.ObjectName);
                        if (objectToDelete != null)
                        {
                            TryDeleteObject(response, objectToDelete);
                        }
                    }
                }
                foreach (var item in SpriteManager.ManagedPositionedObjects)
                {
                    if (CommandReceiver.DoTypesMatch(item, elementGameType, ownerType))
                    {
                        // try to remove this object from here...
                        //screen.ApplyVariable(variableNameOnObjectInInstance, variableValue, item);
                        var objectToDelete = item.Children.FindByName(removeObjectDto.ObjectName);

                        if (objectToDelete != null)
                        {
                            TryDeleteObject(response, objectToDelete);
                        }
                    }
                }
            }
            else
            {
                bool matchesCurrentScreen =
                    (ScreenManager.CurrentScreen.GetType().FullName == elementGameType || ownerType?.IsAssignableFrom(ScreenManager.CurrentScreen.GetType()) == true);

                if (matchesCurrentScreen)
                {
                    response.DidScreenMatch = true;
                    var isEditingEntity =
                        ScreenManager.CurrentScreen?.GetType() == typeof(Screens.EntityViewingScreen);
                    var editingMode = isEditingEntity
                        ? GlueControl.Editing.ElementEditingMode.EditingEntity
                        : GlueControl.Editing.ElementEditingMode.EditingScreen;

                    var foundObject = GlueControl.Editing.SelectionLogic.GetAvailableObjects(editingMode)
                            .FirstOrDefault(item => item.Name == removeObjectDto.ObjectName);
                    TryDeleteObject(response, foundObject);

                    if (!response.WasObjectRemoved)
                    {
                        // see if there is a collision relationship with this name
                        var matchingCollisionRelationship = FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.FirstOrDefault(
                            item => item.Name == removeObjectDto.ObjectName);

                        if (matchingCollisionRelationship != null)
                        {
                            FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Remove(matchingCollisionRelationship);
                            response.WasObjectRemoved = true;
                        }
                    }
                }
            }



            return response;
        }

        private static void TryDeleteObject(RemoveObjectDtoResponse removeResponse, PositionedObject objectToDelete)
        {
            if (objectToDelete is IDestroyable asDestroyable)
            {
                asDestroyable.Destroy();
                removeResponse.WasObjectRemoved = true;
            }
            else if (objectToDelete is AxisAlignedRectangle rectangle)
            {
                ShapeManager.Remove(rectangle);
                removeResponse.WasObjectRemoved = true;
            }
            else if (objectToDelete is Circle circle)
            {
                ShapeManager.Remove(circle);
                removeResponse.WasObjectRemoved = true;
            }
            else if (objectToDelete is Polygon polygon)
            {
                ShapeManager.Remove(polygon);
                removeResponse.WasObjectRemoved = true;
            }
            else if (objectToDelete is Sprite sprite)
            {
                SpriteManager.RemoveSprite(sprite);
                removeResponse.WasObjectRemoved = true;
            }
            else if (objectToDelete is Text text)
            {
                TextManager.RemoveText(text);
                removeResponse.WasObjectRemoved = true;
            }
#if HasGum
            else if (objectToDelete is GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper gumWrapper)
            {
                gumWrapper.GumObject.Destroy();
                gumWrapper.RemoveSelfFromListsBelongingTo();
            }
#endif
        }


        #endregion

        private static void SendAndEnqueue(Dtos.AddObjectDto addObjectDto)
        {
            var currentScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen;
            if (currentScreen is Screens.EntityViewingScreen entityViewingScreen)
            {
                addObjectDto.ElementNameGame = entityViewingScreen.CurrentEntity.GetType().FullName;
            }
            else
            {
                addObjectDto.ElementNameGame = currentScreen.GetType().FullName;
            }

            GlueControlManager.Self.SendToGlue(addObjectDto);

            CommandReceiver.GlobalGlueToGameCommands.Add(addObjectDto);
        }

        #region Create Instance from Game

        private string GetNameFor(string itemType)
        {
            if (itemType.Contains('.'))
            {
                var lastDot = itemType.LastIndexOf('.');
                itemType = itemType.Substring(lastDot + 1);
            }
            var newName = $"{itemType}Auto{TimeManager.CurrentTime.ToString().Replace(".", "_")}_{NewIndex}";
            NewIndex++;

            return newName;
        }

        private void AddFloatValue(Dtos.AddObjectDto addObjectDto, string name, float value)
        {
            AddValue(addObjectDto, name, "float", value);
        }

        private void AddStringValue(Dtos.AddObjectDto addObjectDto, string name, string value)
        {
            AddValue(addObjectDto, name, "string", value);
        }

        private void AddValue(Dtos.AddObjectDto addObjectDto, string name, string type, object value)
        {
            addObjectDto.InstructionSaves.Add(new FlatRedBall.Content.Instructions.InstructionSave
            {
                Member = name,
                Type = type,
                Value = value
            });
        }

        public FlatRedBall.PositionedObject CreateInstanceByGame(string entityGameType, PositionedObject original)
        {
            var newName = GetNameFor(entityGameType);

            var toReturn = CreateEntity(entityGameType);
            toReturn.X = original.X;
            toReturn.Y = original.Y;
            toReturn.Name = newName;

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.Entity;
            // todo - need to eventually include sub namespaces for entities in folders
            addObjectDto.SourceClassType = CommandReceiver.GameElementTypeToGlueElement(entityGameType);

            AddFloatValue(addObjectDto, "X", original.X);
            AddFloatValue(addObjectDto, "Y", original.Y);

            var properties = toReturn.GetType().GetProperties();

            foreach (var property in properties)
            {
                var oldPropertyValue = property.GetValue(original);
                var newPropertyValue = property.GetValue(toReturn);

                if (oldPropertyValue != newPropertyValue)
                {
                    // they differ, so we should set and DTO it
                    // But how do we know what to set and what not to set? I think we should whitelist...

                    var shouldSet = oldPropertyValue != null;
                    var isState = false;
                    if (shouldSet)
                    {
                        // for now we'll only handle states, which have a + in the name. 
                        var fullName = property.PropertyType.FullName;
                        isState = fullName.Contains("+");
                        shouldSet = isState;
                    }

                    if (shouldSet)
                    {
                        property.SetValue(toReturn, oldPropertyValue);
                        var type = property.PropertyType.Name;
                        var value = oldPropertyValue;

                        if (isState)
                        {
                            type = property.PropertyType.FullName.Replace("+", ".");
                            var nameField = property.PropertyType.GetField("Name");
                            if (nameField != null)
                            {
                                value = nameField.GetValue(value);
                            }
                        }

                        AddValue(addObjectDto, property.Name, type, value);
                    }
                }
            }

            #endregion

            SendAndEnqueue(addObjectDto);

            return toReturn;
        }

        public FlatRedBall.PositionedObject CreateInstanceByGame(string entityGameType, float x, float y)
        {
            var newName = GetNameFor(entityGameType);

            var toReturn = CreateEntity(entityGameType);
            toReturn.X = x;
            toReturn.Y = y;
            toReturn.Name = newName;

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.Entity;
            // todo - need to eventually include sub namespaces for entities in folders
            addObjectDto.SourceClassType = CommandReceiver.GameElementTypeToGlueElement(entityGameType);

            AddFloatValue(addObjectDto, "X", x);
            AddFloatValue(addObjectDto, "Y", y);

            //var fields = toReturn.GetType().GetFields();




            #endregion

            SendAndEnqueue(addObjectDto);

            return toReturn;
        }

        public Circle HandleCreateCircleByGame(Circle originalCircle)
        {
            var newCircle = originalCircle.Clone();
            var newName = GetNameFor("Circle");

            newCircle.Visible = originalCircle.Visible;
            newCircle.Name = newName;

            if (ShapeManager.AutomaticallyUpdatedShapes.Contains(newCircle))
            {
                ShapeManager.AddCircle(newCircle);
            }
            InstanceLogic.Self.ShapesAddedAtRuntime.Add(newCircle);

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.FlatRedBallType;
            // todo - need to eventually include sub namespaces for entities in folders
            addObjectDto.SourceClassType = "FlatRedBall.Math.Geometry.Circle";

            AddFloatValue(addObjectDto, "X", newCircle.X);
            AddFloatValue(addObjectDto, "Y", newCircle.Y);
            AddFloatValue(addObjectDto, "Radius", newCircle.Radius);

            #endregion

            SendAndEnqueue(addObjectDto);

            return newCircle;
        }

        public AxisAlignedRectangle HandleCreateAxisAlignedRectangleByGame(AxisAlignedRectangle originalRectangle)
        {
            var newRectangle = originalRectangle.Clone();
            var newName = GetNameFor("Rectangle");

            newRectangle.Visible = originalRectangle.Visible;
            newRectangle.Name = newName;


            if (ShapeManager.AutomaticallyUpdatedShapes.Contains(originalRectangle))
            {
                ShapeManager.AddAxisAlignedRectangle(newRectangle);
            }
            InstanceLogic.Self.ShapesAddedAtRuntime.Add(newRectangle);

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.FlatRedBallType;
            // todo - need to eventually include sub namespaces for entities in folders
            addObjectDto.SourceClassType = "FlatRedBall.Math.Geometry.AxisAlignedRectangle";

            AddFloatValue(addObjectDto, "X", newRectangle.X);
            AddFloatValue(addObjectDto, "Y", newRectangle.Y);
            AddFloatValue(addObjectDto, "Width", newRectangle.Width);
            AddFloatValue(addObjectDto, "Height", newRectangle.Height);

            #endregion

            SendAndEnqueue(addObjectDto);

            return newRectangle;
        }

        public Polygon HandleCreatePolygonByGame(Polygon originalPolygon)
        {
            var newPolygon = originalPolygon.Clone();
            var newName = GetNameFor("Polygon");

            newPolygon.Visible = originalPolygon.Visible;
            newPolygon.Name = newName;

            if (ShapeManager.AutomaticallyUpdatedShapes.Contains(originalPolygon))
            {
                ShapeManager.AddPolygon(newPolygon);
            }
            InstanceLogic.Self.ShapesAddedAtRuntime.Add(newPolygon);

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.FlatRedBallType;
            // todo - need to eventually include sub namespaces for entities in folders
            addObjectDto.SourceClassType = "FlatRedBall.Math.Geometry.Polygon";

            AddFloatValue(addObjectDto, "X", newPolygon.X);
            AddFloatValue(addObjectDto, "Y", newPolygon.Y);

            AddValue(addObjectDto, "Points", typeof(List<Point>).ToString(),
                Newtonsoft.Json.JsonConvert.SerializeObject(newPolygon.Points.ToList()));

            #endregion

            SendAndEnqueue(addObjectDto);

            return newPolygon;
        }

        public Sprite HandleCreateSpriteByName(Sprite originalSprite)
        {
            var newSprite = originalSprite.Clone();
            var newName = GetNameFor("Sprite");

            newSprite.Name = newName;

            if (SpriteManager.AutomaticallyUpdatedSprites.Contains(originalSprite))
            {
                SpriteManager.AddSprite(newSprite);
            }
            InstanceLogic.Self.SpritesAddedAtRuntime.Add(newSprite);

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.FlatRedBallType;
            addObjectDto.SourceClassType = "FlatRedBall.Sprite";

            AddFloatValue(addObjectDto, "X", newSprite.X);
            AddFloatValue(addObjectDto, "Y", newSprite.Y);
            if (newSprite.TextureScale > 0)
            {
                AddFloatValue(addObjectDto, nameof(newSprite.TextureScale), newSprite.TextureScale);
            }
            else
            {
                AddFloatValue(addObjectDto, nameof(newSprite.Width), newSprite.Width);
                AddFloatValue(addObjectDto, nameof(newSprite.Height), newSprite.Height);
            }


            if (newSprite.Texture != null)
            {
                // Texture must be assigned before pixel values.
                AddValue(addObjectDto, "Texture", typeof(Microsoft.Xna.Framework.Graphics.Texture2D).FullName,
                    newSprite.Texture.Name);

                // Glue uses the pixel coords, but we can check the coordinates more easily
                if (newSprite.LeftTextureCoordinate != 0)
                {
                    AddFloatValue(addObjectDto, nameof(newSprite.LeftTexturePixel), newSprite.LeftTexturePixel);
                }
                if (newSprite.TopTextureCoordinate != 0)
                {
                    AddFloatValue(addObjectDto, nameof(newSprite.TopTexturePixel), newSprite.TopTexturePixel);
                }
                if (newSprite.RightTextureCoordinate != 1)
                {
                    AddFloatValue(addObjectDto, nameof(newSprite.RightTexturePixel), newSprite.RightTexturePixel);
                }
                if (newSprite.BottomTextureCoordinate != 1)
                {
                    AddFloatValue(addObjectDto, nameof(newSprite.BottomTexturePixel), newSprite.BottomTexturePixel);
                }
            }
            if (newSprite.AnimationChains?.Name != null)
            {
                AddValue(addObjectDto, "AnimationChains", typeof(FlatRedBall.Graphics.Animation.AnimationChainList).FullName,
                    newSprite.AnimationChains.Name);
            }
            if (!string.IsNullOrEmpty(newSprite.CurrentChainName))
            {
                AddStringValue(addObjectDto, "CurrentChainName", newSprite.CurrentChainName);
            }
            if (newSprite.TextureAddressMode != Microsoft.Xna.Framework.Graphics.TextureAddressMode.Clamp)
            {
                AddValue(addObjectDto, nameof(newSprite.TextureAddressMode),
                    nameof(Microsoft.Xna.Framework.Graphics.TextureAddressMode), (int)newSprite.TextureAddressMode);
            }
            if (newSprite.Red != 0.0f)
            {
                AddFloatValue(addObjectDto, nameof(newSprite.Red), newSprite.Red);
            }
            if (newSprite.Green != 0.0f)
            {
                AddFloatValue(addObjectDto, nameof(newSprite.Green), newSprite.Green);
            }
            if (newSprite.Blue != 0.0f)
            {
                AddFloatValue(addObjectDto, nameof(newSprite.Blue), newSprite.Blue);
            }
            if (newSprite.Alpha != 1.0f)
            {
                AddFloatValue(addObjectDto, nameof(newSprite.Alpha), newSprite.Alpha);
            }
            if (newSprite.ColorOperation != ColorOperation.Texture)
            {
                AddValue(addObjectDto, nameof(newSprite.ColorOperation),
                    nameof(ColorOperation), (int)newSprite.ColorOperation);
            }
            if (newSprite.BlendOperation != BlendOperation.Regular)
            {
                AddValue(addObjectDto, nameof(newSprite.BlendOperation),
                    nameof(BlendOperation), (int)newSprite.BlendOperation);
            }

            // do we want to consider animated sprites? Does it matter?
            // An animation could flip this and that would incorrectly set
            // that value on Glue but if it's animated that would get overwritten anyway, so maybe it's no biggie?
            if (newSprite.FlipHorizontal != false)
            {
                AddValue(addObjectDto, nameof(newSprite.FlipHorizontal),
                    "bool", newSprite.FlipHorizontal);
            }
            if (newSprite.FlipVertical != false)
            {
                AddValue(addObjectDto, nameof(newSprite.FlipVertical),
                    "bool", newSprite.FlipVertical);
            }

            #endregion

            SendAndEnqueue(addObjectDto);

            return newSprite;
        }

        public Text HandleCreateTextByName(Text originalText)
        {
            var newText = originalText.Clone();
            var newName = GetNameFor("Text");

            newText.Name = newName;
            if (TextManager.AutomaticallyUpdatedTexts.Contains(originalText))
            {
                TextManager.AddText(newText);
            }
            InstanceLogic.Self.TextsAddedAtRuntime.Add(newText);

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.FlatRedBallType;
            addObjectDto.SourceClassType = typeof(FlatRedBall.Graphics.Text).FullName;

            AddFloatValue(addObjectDto, "X", newText.X);
            AddFloatValue(addObjectDto, "Y", newText.Y);

            AddValue(addObjectDto, nameof(Text.DisplayText), "string", newText.DisplayText);

            AddValue(addObjectDto, nameof(Text.HorizontalAlignment), nameof(HorizontalAlignment), (int)newText.HorizontalAlignment);
            AddValue(addObjectDto, nameof(Text.VerticalAlignment), nameof(VerticalAlignment), (int)newText.VerticalAlignment);

            if (newText.Red != 0.0f)
            {
                AddFloatValue(addObjectDto, nameof(newText.Red), newText.Red);
            }
            if (newText.Green != 0.0f)
            {
                AddFloatValue(addObjectDto, nameof(newText.Green), newText.Green);
            }
            if (newText.Blue != 0.0f)
            {
                AddFloatValue(addObjectDto, nameof(newText.Blue), newText.Blue);
            }
            if (newText.Alpha != 1.0f)
            {
                AddFloatValue(addObjectDto, nameof(newText.Alpha), newText.Alpha);
            }
            if (newText.ColorOperation != ColorOperation.Texture)
            {
                AddValue(addObjectDto, nameof(newText.ColorOperation),
                    nameof(ColorOperation), (int)newText.ColorOperation);
            }
            if (newText.BlendOperation != BlendOperation.Regular)
            {
                AddValue(addObjectDto, nameof(newText.BlendOperation),
                    nameof(BlendOperation), (int)newText.BlendOperation);
            }

            #endregion

            SendAndEnqueue(addObjectDto);

            return newText;
        }

        #endregion

        #region Delete Instance from Game

        public void DeleteInstanceByGame(INameable instance)
        {
            // Vic June 27, 2021
            // this sends a command to Glue to delete the object, but doesn't
            // actually delete it in game until Glue tells the game to get rid
            // of it. Is that okay? it's a little slower, but it works. Maybe at
            // some point in the future I'll find a reason why it needs to be immediate.
            var name = instance.Name;

            var dto = new Dtos.RemoveObjectDto();
            dto.ObjectName = instance.Name;

            GlueControlManager.Self.SendToGlue(dto);
        }

        #endregion

        public void AssignVariable(object instance, FlatRedBall.Content.Instructions.InstructionSave instruction)
        {
            string variableName = instruction.Member;
            object variableValue = instruction.Value;

            Type stateType = VariableAssignmentLogic.TryGetStateType(instruction.Type);

            if (stateType != null && variableValue is string valueAsString && !string.IsNullOrWhiteSpace(valueAsString))
            {
                var fieldInfo = stateType.GetField(valueAsString);

                variableValue = fieldInfo.GetValue(null);
            }
            else if (instruction.Type == "float" || instruction.Type == "Single")
            {
                if (variableValue is int asInt)
                {
                    variableValue = (float)asInt;
                }
                else if (variableValue is double asDouble)
                {
                    variableValue = (float)asDouble;
                }
            }
            else if (instruction.Type == typeof(FlatRedBall.Graphics.Animation.AnimationChainList).FullName ||
                instruction.Type == typeof(Microsoft.Xna.Framework.Graphics.Texture2D).FullName ||
                instruction.Type == typeof(Microsoft.Xna.Framework.Color).FullName)
            {
                if (variableValue is string asString && !string.IsNullOrWhiteSpace(asString))
                {
                    variableValue = Editing.VariableAssignmentLogic.ConvertStringToType(instruction.Type, asString, false);
                }
            }
            else if (instruction.Type == typeof(Microsoft.Xna.Framework.Graphics.TextureAddressMode).Name)
            {
                if (variableValue is int asInt)
                {
                    variableValue = (Microsoft.Xna.Framework.Graphics.TextureAddressMode)asInt;
                }
                if (variableValue is long asLong)
                {
                    variableValue = (Microsoft.Xna.Framework.Graphics.TextureAddressMode)asLong;
                }
            }

            FlatRedBall.Instructions.Reflection.LateBinder.SetValueStatic(instance, variableName, variableValue);
        }

        public void DestroyDynamicallyAddedInstances()
        {
            for (int i = ShapesAddedAtRuntime.AxisAlignedRectangles.Count - 1; i > -1; i--)
            {
                ShapeManager.Remove(ShapesAddedAtRuntime.AxisAlignedRectangles[i]);
            }

            for (int i = ShapesAddedAtRuntime.Circles.Count - 1; i > -1; i--)
            {
                ShapeManager.Remove(ShapesAddedAtRuntime.Circles[i]);
            }

            for (int i = ShapesAddedAtRuntime.Polygons.Count - 1; i > -1; i--)
            {
                ShapeManager.Remove(ShapesAddedAtRuntime.Polygons[i]);
            }


            for (int i = SpritesAddedAtRuntime.Count - 1; i > -1; i--)
            {
                SpriteManager.RemoveSprite(SpritesAddedAtRuntime[i]);
            }

            for (int i = TextsAddedAtRuntime.Count - 1; i > -1; i--)
            {
                TextManager.RemoveText(TextsAddedAtRuntime[i]);
            }

            for (int i = DestroyablesAddedAtRuntime.Count - 1; i > -1; i--)
            {
                DestroyablesAddedAtRuntime[i].Destroy();
            }

            foreach (var list in ListsAddedAtRuntime)
            {
                for (int i = list.Count - 1; i > -1; i--)
                {
                    var positionedObject = list[i] as PositionedObject;
                    positionedObject.RemoveSelfFromListsBelongingTo();
                }
            }

#if HasGum

            for (int i = GumObjectsAddedAtRuntime.Count - 1; i > -1; i--)
            {
                GumObjectsAddedAtRuntime[i].Destroy();
            }
            for(int i = GumWrappersAddedAtRuntime.Count - 1; i > -1; i--)
            {
                GumWrappersAddedAtRuntime[i].RemoveSelfFromListsBelongingTo();
            }
            GumObjectsAddedAtRuntime.Clear();
            GumWrappersAddedAtRuntime.Clear();
#endif

            ShapesAddedAtRuntime.Clear();
            SpritesAddedAtRuntime.Clear();
            DestroyablesAddedAtRuntime.Clear();
            ListsAddedAtRuntime.Clear();
            TextsAddedAtRuntime.Clear();
        }
    }
}
