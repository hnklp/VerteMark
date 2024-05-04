using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using VerteMark.ObjectClasses.FolderClasses;

namespace VerteMark.ObjectClasses {
    /// <summary>
    /// Třída obsahující metody, které slouží jako interface pro UI
    /// (Prakticky metody co se volají tlačítky)
    /// </summary>
    internal class Utility {
        // Vlastnosti
        Project project;

        // Konstruktor
        public Utility() {
            project = Project.GetInstance();
        }


        // fotka krku
        public BitmapImage? GetOriginalPicture() {
            return project.GetOriginalPicture();
        }

        // Returns true if project was loaded (in any way), returns false if loading has failed
        public bool ChooseProjectFolder(string path) {
            return project.TryOpeningProject(path);
        }

        public void SaveProject() {
            project.SaveProject();
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
            project.ValidateAnnotationByID(id);
        }

        public void ChangeActiveAnotation(int id) {
            project.SelectActiveAnotace(id);
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

        public User? GetLoggedInUser() {
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
    }
}
