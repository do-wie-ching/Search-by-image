﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Diagnostics;

namespace TestOnnx
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Using API");
            UseApi();
            Console.WriteLine("Done");
        }


        static void UseApi()
        {
            string basepath = "..\\..\\..\\testdata\\";
            string modelPath = basepath + "squeezenet.onnx";
            Debug.Assert(File.Exists(modelPath));
            // Optional : Create session options and set the graph optimization level for the session
            SessionOptions options = new SessionOptions();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED;

            using (var session = new InferenceSession(modelPath, options))
            {
                var inputMeta = session.InputMetadata;
                var container = new List<NamedOnnxValue>();

                float[] inputData = LoadTensorFromFile(basepath + "bench.in"); // this is the data for only one input tensor for this model

                foreach (var name in inputMeta.Keys)
                {
                    var tensor = new DenseTensor<float>(inputData, inputMeta[name].Dimensions);
                    container.Add(NamedOnnxValue.CreateFromTensor<float>(name, tensor));
                }

                // Run the inference
                using (var results = session.Run(container))  // results is an IDisposableReadOnlyCollection<DisposableNamedOnnxValue> container
                {
                    // dump the results
                    foreach (var r in results)
                    {
                        Console.WriteLine("Output for {0}", r.Name);
                        Console.WriteLine(r.AsTensor<float>().GetArrayString());
                    }
                }
            }
        }

        static float[] LoadTensorFromFile(string filename)
        {
            var tensorData = new List<float>();

            // read data from file
            using (var inputFile = new System.IO.StreamReader(filename))
            {
                inputFile.ReadLine(); //skip the input name
                string[] dataStr = inputFile.ReadLine().Split(new char[] { ',', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < dataStr.Length; i++)
                {
                    tensorData.Add(Single.Parse(dataStr[i]));
                }
            }

            return tensorData.ToArray();
        }


    }
}
