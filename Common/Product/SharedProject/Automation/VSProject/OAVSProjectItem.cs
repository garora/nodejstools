//*********************************************************//
//    Copyright (c) Microsoft. All rights reserved.
//    
//    Apache 2.0 License
//    
//    You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//    
//    Unless required by applicable law or agreed to in writing, software 
//    distributed under the License is distributed on an "AS IS" BASIS, 
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or 
//    implied. See the License for the specific language governing 
//    permissions and limitations under the License.
//
//*********************************************************//

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EnvDTE;
using VSLangProj;

namespace Microsoft.VisualStudioTools.Project.Automation
{
    /// <summary>
    /// Represents a language-specific project item
    /// </summary>
    [ComVisible(true)]
    public class OAVSProjectItem : VSProjectItem
    {
        #region fields
        private FileNode fileNode;
        #endregion

        #region ctors
        internal OAVSProjectItem(FileNode fileNode)
        {
            this.FileNode = fileNode;
        }
        #endregion

        #region VSProjectItem Members

        public virtual EnvDTE.Project ContainingProject
        {
            get { return fileNode.ProjectMgr.GetAutomationObject() as EnvDTE.Project; }
        }

        public virtual ProjectItem ProjectItem
        {
            get { return fileNode.GetAutomationObject() as ProjectItem; }
        }

        public virtual DTE DTE
        {
            get { return (DTE)this.fileNode.ProjectMgr.Site.GetService(typeof(DTE)); }
        }

        public void RunCustomTool()
        {
        }

        #endregion

        #region public properties
        /// <summary>
        /// File Node property
        /// </summary>
        internal FileNode FileNode
        {
            get
            {
                return fileNode;
            }
            set
            {
                fileNode = value;
            }
        }
        #endregion

    }
}
