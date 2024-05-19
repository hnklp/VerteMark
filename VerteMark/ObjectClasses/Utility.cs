using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;

namespace VerteMark.ObjectClasses
{
    /// <summary>
    /// Třída obsahující metody, které slouží jako interface pro UI
    /// (Prakticky metody co se volají tlačítky)
    /// </summary>
    internal class Utility {
        // Vlastnosti
        Project project;
        public bool saved;

        // Konstruktor
        public Utility() {
            project = Project.GetInstance();
            saved = false;
        }

        public void CropOriginalPicture(BitmapSource image) {
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream()) {
                PngBitmapEncoder encoder = new PngBitmapEncoder(); // nebo jiný vhodný encoder podle vašich potřeb
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }
            project.SetOriginalPicture(bitmapImage);
        }


        // fotka krku
        public BitmapImage? GetOriginalPicture() {
            return project.GetOriginalPicture();
        }

        // Returns true if project was loaded (in any way), returns false if loading has failed
        public bool ChooseProjectFolder(string path) {
            return project.TryOpeningProject(path);
        }

        public void SaveProject(int savingParameter) {
            project.SaveProject(savingParameter);
        }

        public WriteableBitmap GetActiveAnotaceImage() {
            return project.ActiveAnotaceImage();
        }

        public void UpdateSelectedAnotation(WriteableBitmap bitmap) {
            project.UpdateSelectedAnotaceCanvas(bitmap);
        }

        public string GetActiveAnotaceId() {
            return project.ActiveAnotaceId();
        }

        public System.Windows.Media.Color GetActiveAnotaceColor() {
            return project.ActiveAnotaceColor();
        }

        public void ClearActiveAnotace() {
            project.ClearActiveAnotace();
        }

        public void SwitchAnotationValidation(int id) {
            Debug.Write("UTILITY VOLA VALIDACI");
            project.ValidateAnnotationByID(id);
        }

        public void ChangeActiveAnotation(int id) {
            project.SelectActiveAnotace(id);
        }

        public void ValidateAll() {
            project.ValidateAll();
        }

        public List<Anotace> GetAnnotationsList(){
            return project.GetAnotaces();
        }

        public Anotace CreateImplantAnnotation(){
            return project.CreateImplantAnnotation();
        }

        /*
        * ==================
        * Prace s uzivatelem
        * ==================
        */

        public void LoginUser(string id, bool validator) {
            project.LoginNewUser(id, validator);
        }

        public void LogoutUser() {
            project.LogoutUser();
        }

        public List<WriteableBitmap> AllInactiveAnotaceImages() {
            return project.AllInactiveAnotaceImages();
        }

        public User GetLoggedInUser() {
            return project.GetLoggedInUser();
        }

        /*
        * =============================
        * Pouziti v FolderbrowserWindow
        * =============================
        */

        public List<string> ChooseNewProject()
        {
            return project.ChooseNewProject();
        }

        public List<string> ChooseContinueAnotation()
        {
            return project.ChooseContinueAnotation();
        }

        public List<string> ChooseValidation()
        {
            return project.ChooseValidation();
        }

        public void Choose(string path, string projectType)
        {
            project.Choose(path, projectType);
        }

        public bool GetIsAnotated()
        {
            return project.GetIsAnotated();
        }

        public void SetActiveAnotaceIsAnotated(bool isAnotated)
        {
            project.SetActiveAnotaceIsAnotated(isAnotated);
        }
        // debug

        public void CreateNewProjectDEBUG() {
            project.CreateNewProjectDEBUG();
        }

    }
}
