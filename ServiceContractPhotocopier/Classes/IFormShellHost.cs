using System.Windows.Forms;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Implemented by a host form (e.g. the dev launcher's tabbed shell) that wants
    /// to receive child-form open requests from list forms instead of those list
    /// forms popping a modal dialog. List forms detect the host via
    /// <c>this.FindForm() as IFormShellHost</c>; if non-null, route to the host;
    /// otherwise fall back to <c>ShowDialog</c> for the production AutoCount mode.
    /// </summary>
    public interface IFormShellHost
    {
        /// <summary>
        /// Open a form by its catalog title in a new tab (or focus existing).
        /// Used for "+ New" buttons where no row context is needed.
        /// </summary>
        void OpenFormByTitle(string title);

        /// <summary>
        /// Embed a pre-constructed form into a new tab (or focus existing tab
        /// with the same title). Used for "Edit" buttons that need to pass a
        /// row / key into a constructor the catalog can't express.
        /// </summary>
        void OpenFormInTab(string tabTitle, Form form);
    }
}
