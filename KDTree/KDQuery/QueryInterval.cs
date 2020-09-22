/*MIT License

Copyright(c) 2018 Vili Volčini / viliwonka

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections.Generic;
using System;
using VRageMath;
using TestingTaskFramework;

namespace DataStructures.ViliWonka.KDTree {

    public partial class KDQuery {

        public void Interval(KDTree tree, Vector3 min, Vector3 max, List<int> resultIndices) {

            Reset();

            Vector3[] points = tree.Points;
            int[] permutation = tree.Permutation;

            var rootNode = tree.RootNode;

            PushToQueue(

                rootNode,
                max - min
            );

            KDQueryNode queryNode = null;
            KDNode node = null;


            // KD search with pruning (don't visit areas which distance is more away than range)
            // Recursion done on Stack
            while(LeftToProcess > 0) {

                queryNode = PopFromQueue();
                node = queryNode.node;

                if(!node.Leaf) {

                    int partitionAxis = node.partitionAxis;
                    float partitionCoord = node.partitionCoordinate;

                    Vector3 tempClosestPoint = queryNode.tempClosestPoint;

                    float value = 0;
                    if (partitionAxis == 1)
                        value = tempClosestPoint.X;
                    else if (partitionAxis == 2)
                        value = tempClosestPoint.Y;
                    else if (partitionAxis == 3)
                        value = tempClosestPoint.Z;

                    float maxValue = 0;
                    if (partitionAxis == 1)
                        maxValue = max.X;
                    else if (partitionAxis == 2)
                        maxValue = max.Y;
                    else if (partitionAxis == 3)
                        maxValue = max.Z;

                    if ((value - partitionCoord) < 0) {

                        // we already know we are inside negative bound/node,
                        // so we don't need to test for distance
                        // push to stack for later querying

                        // tempClosestPoint is inside negative side
                        // assign it to negativeChild
                        PushToQueue(node.negativeChild, tempClosestPoint);

                        if (partitionAxis == 1)
                            tempClosestPoint.X = partitionCoord;
                        else if (partitionAxis == 2)
                            tempClosestPoint.Y = partitionCoord;
                        else if (partitionAxis == 3)
                            tempClosestPoint.Z = partitionCoord;

                        // testing other side
                        if (node.positiveChild.Count != 0
                        && value <= maxValue) {

                            PushToQueue(node.positiveChild, tempClosestPoint);
                        }
                    }
                    else {

                        // we already know we are inside positive bound/node,
                        // so we don't need to test for distance
                        // push to stack for later querying

                        // tempClosestPoint is inside positive side
                        // assign it to positiveChild
                        PushToQueue(node.positiveChild, tempClosestPoint);

                        // project the tempClosestPoint to other bound
                        if (partitionAxis == 1)
                            tempClosestPoint.X = partitionCoord;
                        else if (partitionAxis == 2)
                            tempClosestPoint.Y = partitionCoord;
                        else if (partitionAxis == 3)
                            tempClosestPoint.Z = partitionCoord;

                        if (partitionAxis == 1)
                            value = tempClosestPoint.X;
                        else if (partitionAxis == 2)
                            value = tempClosestPoint.Y;
                        else if (partitionAxis == 3)
                            value = tempClosestPoint.Z;

                        float minValue = 0;
                        if (partitionAxis == 1)
                            minValue = min.X;
                        else if (partitionAxis == 2)
                            minValue = min.Y;
                        else if (partitionAxis == 3)
                            minValue = min.Z;

                        // testing other side
                        if (node.negativeChild.Count != 0
                        && value >= minValue) {

                            PushToQueue(node.negativeChild, tempClosestPoint);
                        }
                    }
                }
                else {

                    // LEAF

                    // testing if node bounds are inside the query interval
                    if(node.bounds.min.X >= min.X
                    && node.bounds.min.Y >= min.Y
                    && node.bounds.min.Z >= min.Z

                    && node.bounds.max.X <= max.X
                    && node.bounds.max.Y <= max.Y
                    && node.bounds.max.Z <= max.Z) {

                        for(int i = node.start; i < node.end; i++) {

                            resultIndices.Add(permutation[i]);
                        }

                    }
                    // node is not inside query interval, need to do test on each point separately
                    else {

                        for(int i = node.start; i < node.end; i++) {

                            int index = permutation[i];

                            Vector3 v = points[index];

                            if(v.X >= min.X
                            && v.Y >= min.Y
                            && v.Z >= min.Z

                            && v.X <= max.X
                            && v.Y <= max.Y
                            && v.Z <= max.Z) {

                                resultIndices.Add(index);
                            }
                        }
                    }

                }
            }
        }
    }

}