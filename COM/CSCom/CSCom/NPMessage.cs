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
        public NPMessage(int type, NPMessageNamepathData[] data, string message = null)
            : this((NPMessageType)type, data, message)
        {
        }

        /// <summary>
        /// Make a JMessage from namepaths data
        /// </summary>
        /// <param name="namepaths"></param>
        /// <param name="values"></param>
        public NPMessage(NPMessageType type, NPMessageNamepathData[] data, string message = null)
        {
            if (data == null)
                data = new NPMessageNamepathData[0];

            m_NamePaths = data;
            Message = message;
            MessageType = type;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The message type.
        /// </summary>
        public NPMessageType MessageType { get; private set; } = NPMessageType.Data;

        /// <summary>
        /// The string message to send.
        /// </summary>
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

        Error = 1,
        Warning = 2,
        Create = 4,
        Destroy = 8,
        Invoke = 16,
        Get = 32,
        Set = 64,
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
        public int[] Idxs;

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
