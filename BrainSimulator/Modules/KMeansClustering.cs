using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator.Modules
{
    class KMeansClustering
    {
        public static int[] Cluster(double[][] rawData, int numClusters)
        {
            // k-means clustering
            //from https://visualstudiomagazine.com/Articles/2013/12/01/K-Means-Data-Clustering-Using-C.aspx

            // index of return is tuple ID, cell is cluster ID
            // ex: [2 1 0 0 2 2] means tuple 0 is cluster 2, tuple 1 is cluster 1, tuple 2 is cluster 0, tuple 3 is cluster 0, etc.
            // an alternative clustering DS to save space is to use the .NET BitArray class
            double[][] data = Normalized(rawData); // so large values don't dominate

            bool changed = true; // was there a change in at least one cluster assignment?
            bool success = true; // were all means able to be computed? (no zero-count clusters)

            // init clustering[] to get things started
            // an alternative is to initialize means to randomly selected tuples
            // then the processing loop is
            // loop
            //    update clustering
            //    update means
            // end loop
            int[] clustering = InitClustering(data.Length, numClusters, 0); // semi-random initialization
            double[][] means = Allocate(numClusters, data[0].Length); // small convenience

            int maxCount = data.Length * 10; // sanity check
            int ct = 0;
            while (changed == true && success == true && ct < maxCount)
            {
                ++ct; // k-means typically converges very quickly
                success = UpdateMeans(data, clustering, means); // compute new cluster means if possible. no effect if fail
                changed = UpdateClustering(data, clustering, means); // (re)assign tuples to clusters. no effect if fail
            }
            // consider adding means[][] as an out parameter - the final means could be computed
            // the final means are useful in some scenarios (e.g., discretization and RBF centroids)
            // and even though you can compute final means from final clustering, in some cases it
            // makes sense to return the means (at the expense of some method signature uglinesss)
            //
            // another alternative is to return, as an out parameter, some measure of cluster goodness
            // such as the average distance between cluster means, or the average distance between tuples in 
            // a cluster, or a weighted combination of both
            return clustering;
        }

        private static double[][] Normalized(double[][] rawData)
        {
            // normalize raw data by computing (x - mean) / stddev
            // primary alternative is min-max:
            // v' = (v - min) / (max - min)

            // make a copy of input data
            double[][] result = new double[rawData.Length][];
            for (int i = 0; i < rawData.Length; ++i)
            {
                result[i] = new double[rawData[i].Length];
                Array.Copy(rawData[i], result[i], rawData[i].Length);
            }

            for (int j = 0; j < result[0].Length; ++j) // each col
            {
                double colSum = 0.0;
                for (int i = 0; i < result.Length; ++i)
                    colSum += result[i][j];
                double mean = colSum / result.Length;
                double sum = 0.0;
                for (int i = 0; i < result.Length; ++i)
                    sum += (result[i][j] - mean) * (result[i][j] - mean);
                double sd = sum / result.Length;
                for (int i = 0; i < result.Length; ++i)
                    result[i][j] = (result[i][j] - mean) / sd;
            }
            return result;
        }

        private static int[] InitClustering(int numTuples, int numClusters, int randomSeed)
        {
            // init clustering semi-randomly (at least one tuple in each cluster)
            // consider alternatives, especially k-means++ initialization,
            // or instead of randomly assigning each tuple to a cluster, pick
            // numClusters of the tuples as initial centroids/means then use
            // those means to assign each tuple to an initial cluster.
            Random random = new Random(randomSeed);
            int[] clustering = new int[numTuples];
            for (int i = 0; i < numClusters; ++i) // make sure each cluster has at least one tuple
                clustering[i] = i;
            for (int i = numClusters; i < clustering.Length; ++i)
                clustering[i] = random.Next(0, numClusters); // other assignments random
            return clustering;
        }

        private static double[][] Allocate(int numClusters, int numColumns)
        {
            // convenience matrix allocator for Cluster()
            double[][] result = new double[numClusters][];
            for (int k = 0; k < numClusters; ++k)
                result[k] = new double[numColumns];
            return result;
        }

        private static bool UpdateMeans(double[][] data, int[] clustering, double[][] means)
        {
            // returns false if there is a cluster that has no tuples assigned to it
            // parameter means[][] is really a ref parameter

            // check existing cluster counts
            // can omit this check if InitClustering and UpdateClustering
            // both guarantee at least one tuple in each cluster (usually true)
            int numClusters = means.Length;
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false; // bad clustering. no change to means[][]

            // update, zero-out means so it can be used as scratch matrix 
            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                    means[k][j] = 0.0;

            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                for (int j = 0; j < data[i].Length; ++j)
                    means[cluster][j] += data[i][j]; // accumulate sum
            }

            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                    means[k][j] /= clusterCounts[k]; // danger of div by 0
            return true;
        }

        private static bool UpdateClustering(double[][] data, int[] clustering, double[][] means)
        {
            // (re)assign each tuple to a cluster (closest mean)
            // returns false if no tuple assignments change OR
            // if the reassignment would result in a clustering where
            // one or more clusters have no tuples.

            int numClusters = means.Length;
            bool changed = false;

            int[] newClustering = new int[clustering.Length]; // proposed result
            Array.Copy(clustering, newClustering, clustering.Length);

            double[] distances = new double[numClusters]; // distances from curr tuple to each mean

            for (int i = 0; i < data.Length; ++i) // walk thru each tuple
            {
                for (int k = 0; k < numClusters; ++k)
                    distances[k] = Distance(data[i], means[k]); // compute distances from curr tuple to all k means

                int newClusterID = MinIndex(distances); // find closest mean ID
                if (newClusterID != newClustering[i])
                {
                    changed = true;
                    newClustering[i] = newClusterID; // update
                }
            }

            if (changed == false)
                return false; // no change so bail and don't update clustering[][]

            // check proposed clustering[] cluster counts
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = newClustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false; // bad clustering. no change to clustering[][]

            Array.Copy(newClustering, clustering, newClustering.Length); // update
            return true; // good clustering and at least one change
        }

        private static double Distance(double[] tuple, double[] mean)
        {
            // Euclidean distance between two vectors for UpdateClustering()
            // consider alternatives such as Manhattan distance
            double sumSquaredDiffs = 0.0;
            for (int j = 0; j < tuple.Length; ++j)
                sumSquaredDiffs += Math.Pow((tuple[j] - mean[j]), 2);
            return Math.Sqrt(sumSquaredDiffs);
        }

        private static int MinIndex(double[] distances)
        {
            // index of smallest value in array
            // helper for UpdateClustering()
            int indexOfMin = 0;
            double smallDist = distances[0];
            for (int k = 0; k < distances.Length; ++k)
            {
                if (distances[k] < smallDist)
                {
                    smallDist = distances[k];
                    indexOfMin = k;
                }
            }
            return indexOfMin;
        }

    }
}
