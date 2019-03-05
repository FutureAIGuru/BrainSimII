using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;

namespace Touchless.Vision.Contracts
{
    [DataContract]
    public class Frame
    {
        [DataMember]
        public int Id { get; set; }

        [IgnoreDataMember]
        public Bitmap OriginalImage { get; set; }

        private Bitmap _image;
        [IgnoreDataMember]
        public Bitmap Image
        {
            get
            {
                if (_image == null)
                {
                    _image = new Bitmap( OriginalImage );
                }

                return _image;
            }
            set { _image = value;}
        }

        public Frame(Bitmap originalImage)
        {
            Id = NextId();
            OriginalImage = new Bitmap( originalImage );
        }

        [DataMember]
        public byte[] ImageData
        {
            get
            {
                byte[] data = null;

                if (Image != null)
                   using( MemoryStream memoryStream = new MemoryStream() )
                   {
                      Image.Save( memoryStream, ImageFormat.Png );
                      memoryStream.Flush();
                      data = memoryStream.ToArray();
                   }

                return data;
            }
            //Setter is only here for serialization purposes
            set { }
        }


        private static readonly object SyncObject = new object();
        private static int _nextId = 1;
        private static int NextId()
        {
            int result;
            lock (SyncObject)
            {
                result = _nextId;
                _nextId++;
            }

            return result;
        }
    }
}