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

        public ObjectId? Id { get; set; } //is null until added

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

        public bool Equals(ModelBase? other) => other != null && other.Id == Id;

        public override bool Equals(object? obj)
        {
            return Equals(obj as ModelBase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }


        public static ObjectId NewId() => ObjectId.NewObjectId();
        public static ObjectId? IdFromString(string? id) => string.IsNullOrWhiteSpace(id) ? default : new(id);

        public static bool operator ==(ModelBase? left, ModelBase? right)
        {
            //return EqualityComparer<ModelBase>.Default.Equals(left, right);
            return (left?.Equals(right)).GetValueOrDefault();
        }

        public static bool operator !=(ModelBase? left, ModelBase? right)
        {
            //return !(left == right);
            return !(left?.Equals(right)).GetValueOrDefault();
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
