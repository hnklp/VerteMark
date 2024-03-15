using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

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
            project = new Project();
        }

        public void LoginUser(string id, bool validator) {
            project.LoginNewUser(id, validator);
        }
        public void LogoutUser() {
            project.LogoutUser();
        }
        public User? GetLoggedInUser() {
            return project.GetLoggedInUser();
        }
        public BitmapImage? GetOriginalPicture() {
            return project.GetOriginalPicture();
        }

        // Returns true if project was loaded (in any way), returns false if loading has failed
        public bool ChooseProjectFolder(string path) {
            return project.TryOpeningProject(path);
        }
        public void SaveProject() {
             
        }
        public void ChangeSelectedAnotation(int id) {

        }
        public void UpdatedSelectedAnotation(int idAnotace) {
            project.UpdateAnotaceCanvas(idAnotace);
        }
        public void ClearSelectedAnotation() {

        }
        public void SwitchAnotationValidation(int id) {

        }
        // Vrátí JPEG/PNG toho krku aby se to mohlo načíst
        public void GetMainPicture() {

        }

    }
}
