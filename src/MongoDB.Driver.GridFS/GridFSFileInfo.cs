﻿/* Copyright 2015 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents information about a stored GridFS file (backed by a files collection document).
    /// </summary>
    [BsonSerializer(typeof(GridFSFileInfoSerializer))]
    public class GridFSFileInfo : BsonDocumentBackedClass
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GridFSFileInfo"/> class.
        /// </summary>
        /// <param name="backingDocument">The backing document.</param>
        public GridFSFileInfo(BsonDocument backingDocument)
            : base(backingDocument, GridFSFileInfoSerializer.Instance)
        {
        }

        // public properties
        /// <summary>
        /// Gets the aliases.
        /// </summary>
        /// <value>
        /// The aliases.
        /// </value>
        [Obsolete("Place aliases inside metadata instead.")]
        public IEnumerable<string> Aliases
        {
            get { return GetValue<string[]>("Aliases", null); }
        }

        /// <summary>
        /// Gets the backing document.
        /// </summary>
        /// <value>
        /// The backing document.
        /// </value>
        new public BsonDocument BackingDocument
        {
            get { return base.BackingDocument; }
        }

        /// <summary>
        /// Gets the size of a chunk.
        /// </summary>
        /// <value>
        /// The size of a chunk.
        /// </value>
        public int ChunkSizeBytes
        {
            get { return GetValue<int>("ChunkSizeBytes"); }
        }

        /// <summary>
        /// Gets the type of the content.
        /// </summary>
        /// <value>
        /// The type of the content.
        /// </value>
        [Obsolete("Place contentType inside metadata instead.")]
        public string ContentType
        {
            get { return GetValue<string>("ContentType", null); }
        }

        /// <summary>
        /// Gets the filename.
        /// </summary>
        /// <value>
        /// The filename.
        /// </value>
        public string Filename
        {
            get { return GetValue<string>("Filename"); }
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public ObjectId Id
        {
            get { return GetValue<BsonValue>("IdAsBsonValue").AsObjectId; }
        }

        /// <summary>
        /// Gets the identifier as a BsonValue.
        /// </summary>
        /// <value>
        /// The identifier as a BsonValue.
        /// </value>
        [Obsolete("All new GridFS files should use an ObjectId as the Id.")]
        public BsonValue IdAsBsonValue
        {
            get { return GetValue<BsonValue>("IdAsBsonValue"); }
        }

        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        public long Length
        {
            get { return GetValue<long>("Length"); }
        }

        /// <summary>
        /// Gets the MD5 checksum.
        /// </summary>
        /// <value>
        /// The MD5 checksum.
        /// </value>
        public string MD5
        {
            get { return GetValue<string>("MD5", null); }
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <value>
        /// The metadata.
        /// </value>
        public BsonDocument Metadata
        {
            get { return GetValue<BsonDocument>("Metadata", null); }
        }

        /// <summary>
        /// Gets the upload date time.
        /// </summary>
        /// <value>
        /// The upload date time.
        /// </value>
        public DateTime UploadDateTime
        {
            get { return GetValue<DateTime>("UploadDateTime"); }
        }
    }

    /// <summary>
    /// Represents a serializer for GridFSFileInfo.
    /// </summary>
    public class GridFSFileInfoSerializer : BsonDocumentBackedClassSerializer<GridFSFileInfo>
    {
        #region static
        // public static properties
        /// <summary>
        /// Gets the pre-created instance.
        /// </summary>
        /// <value>
        /// The pre-created instance.
        /// </value>
        public static GridFSFileInfoSerializer Instance { get; } = new GridFSFileInfoSerializer();
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="GridFSFileInfoSerializer"/> class.
        /// </summary>
        public GridFSFileInfoSerializer()
        {
            RegisterMember("Aliases", "aliases", new ArraySerializer<string>());
            RegisterMember("ChunkSizeBytes", "chunkSize", new Int32Serializer());
            RegisterMember("ContentType", "contentType", new StringSerializer());
            RegisterMember("Filename", "filename", new StringSerializer());
            RegisterMember("IdAsBsonValue", "_id", BsonValueSerializer.Instance);
            RegisterMember("Length", "length", new Int64Serializer());
            RegisterMember("MD5", "md5", new StringSerializer());
            RegisterMember("Metadata", "metadata", BsonDocumentSerializer.Instance);
            RegisterMember("UploadDateTime", "uploadDate", new DateTimeSerializer());
        }

        /// <inheritdoc/>
        protected override GridFSFileInfo CreateInstance(BsonDocument backingDocument)
        {
            return new GridFSFileInfo(backingDocument);
        }
    }
}
