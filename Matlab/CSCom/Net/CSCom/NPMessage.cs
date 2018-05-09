using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSCom
{
    /// <summary>
    /// Implements a data structure for the np object map.
    /// </summary>
    [Serializable]
    public class NPMessage
    {

        #region Construction

        /// <summary>
        /// Make a JMessage from namepaths data
        /// </summary>
        /// <param name="namepaths"></param>
        /// <param name="values"></param>
        public NPMessage(NPMessageType type, NPMessageNamepathData[] data, string message = null)
        {
            m_NamePaths = data;
            Message = message;
            MessageType = MessageType;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The message type.
        /// </summary>
        public NPMessageType MessageType { get; private set; } = NPMessageType.AsString;

        public string Message { get; private set; } = null;

        NPMessageNamepathData[] m_NamePaths;

        public NPMessageNamepathData[] NamePaths
        {
            get { return m_NamePaths; }
        }

        [NonSerialized]
        Dictionary<string, NPMessageNamepathData> m_namePathsDictionary = null;

        /// <summary>
        /// A tuple of (Namepath, Object data) that represents namepaths replaced in the large data structure.
        /// </summary>
        public IReadOnlyDictionary<string, NPMessageNamepathData> Namepaths
        {
            get
            {
                if (m_namePathsDictionary == null)
                {
                    m_namePathsDictionary = new Dictionary<string, NPMessageNamepathData>();
                    foreach (NPMessageNamepathData info in m_NamePaths)
                    {
                        m_namePathsDictionary[info.Namepath] = info;
                    }
                }

                return m_namePathsDictionary;
            }
        }

        #endregion
    }

    public enum NPMessageType : int
    {
        Invoke = 1,
        AsString = 2
    }

    /// <summary>
    /// Implements a specific nampath data.
    /// </summary>
    [Serializable]
    public class NPMessageNamepathData
    {
        /// <summary>
        /// The value of the namepath
        /// </summary>
        public object Value;

        /// <summary>
        /// The idxs being sent, in the case of partial updates.
        /// </summary>
        public int[] idxs;

        /// <summary>
        /// The the original size of the data sent, in case of a matrix.
        /// </summary>
        public int[] Size;

        /// <summary>
        /// The namepath of the object.
        /// </summary>
        public string Namepath;
    }
}
