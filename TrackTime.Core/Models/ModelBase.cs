using LiteDB;

using System;
using System.Collections.Generic;
using System.Text;

namespace TrackTime.Models
{
    public class ModelBase : IEquatable<ModelBase>, IComparable<ModelBase>
    {
        public ModelBase()
        {
        }

        public ObjectId Id { get; set; } = ObjectId.Empty; //is empty until added

        public int CompareTo(ModelBase? other)
        {
            if (Equals(other))
                return 0;

            if (other == null)
                return 1;

            if (Id == null)
                return -1;

            return Id.CompareTo(other.Id);
        }

        public bool Equals(ModelBase? other)
        {
            if (other == null)
                return false;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            var model = obj as ModelBase;
            if (model != null)
                return Equals(model);
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }


        public static ObjectId NewId() => ObjectId.NewObjectId();
        public static ObjectId IdFromString(string id) => string.IsNullOrWhiteSpace(id) ? ObjectId.Empty : new(id);

        public static bool operator == (ModelBase? left, ModelBase? right)
        {
            if (object.ReferenceEquals(left, right))
                return true;
            if (object.ReferenceEquals(left, null)) 
                return false;
            if (object.ReferenceEquals(right, null))
                return false;
            return left.Equals(right);
        }

        public static bool operator !=(ModelBase? left, ModelBase? right)
        {
            if (object.ReferenceEquals(left, right))
                return false;
            if (object.ReferenceEquals(left, null))
                return true;
            if (object.ReferenceEquals(right, null))
                return true;
            return !left.Equals(right);
        }

        public static bool operator <(ModelBase left, ModelBase right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(ModelBase left, ModelBase right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(ModelBase left, ModelBase right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(ModelBase left, ModelBase right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}
