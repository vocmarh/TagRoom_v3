using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagRoom_v3
{
    public class MainViewViewModel
    {
        private ExternalCommandData _commandData;

        public List<RoomTagType> TagType { get; } = new List<RoomTagType>();
        public List<Level> Level { get; } = new List<Level>();
        public DelegateCommand SaveCommand { get; }

        public RoomTagType SelectedTagType { get; set; }

        public Level SelectedLevel { get; set; }

        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            Document doc = commandData.Application.ActiveUIDocument.Document;

            TagType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_RoomTags)
                .Cast<RoomTagType>()
                .ToList();

            Level = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Levels)
                .OfType<Level>()
                .ToList();

            SaveCommand = new DelegateCommand(OnSaveCommand);
        }

        private void OnSaveCommand()
        {
            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            List<Room> roomLevels = new FilteredElementCollector(doc)
                   .OfCategory(BuiltInCategory.OST_Rooms)
                   .WhereElementIsNotElementType()
                   .Cast<Room>()
                   .ToList();

            if (SelectedLevel == null)
            {
                TaskDialog.Show("Ошибка", "Выберите уровень помещения");
                return;
            }

            List<Room> rooms = new List<Room>();
            foreach (Room room in roomLevels)
            {
                Parameter roomLevel = room.get_Parameter(BuiltInParameter.LEVEL_NAME);
                var level = roomLevel.AsString();

                if (level == SelectedLevel.Name)
                    rooms.Add(room);
            }

            if (rooms.Count == 0)
            {
                TaskDialog.Show("Ошибка", "На уровне нет помещения");
                return;
            }

            using (var ts = new Transaction(doc, "Проставление марок"))
            {
                ts.Start();

                foreach (Room room in rooms)
                {
                    LocationPoint location = room.Location as LocationPoint;
                    UV point = new UV(location.Point.X, location.Point.Y);
                    RoomTag newTag = doc.Create.NewRoomTag(new LinkElementId(room.Id), point, null);
                    newTag.RoomTagType = SelectedTagType;
                }

                ts.Commit();
            }
            RaiseCloseRequest();

        }
        public event EventHandler CloseRequest;
        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }

    }


}
