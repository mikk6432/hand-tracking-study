# public static float? CalcStepFrequency(Frame<int, string> data)
# {
#     Series<int, float> timestamps = data.GetColumn<float>("EyeTrackerElapsedTime");
#     Series<int, float> headPositionsY = ApplyOfflineWeightedAverage(data.GetColumn<float>("HeadPositionY"), windowSize: 5, LinearKernel, timestamps);

#     // I. Find all local minima
#     List<int> minimaKeys = new();
#     foreach (KeyValuePair<int, float> kvp in headPositionsY.Observations)
#     {
#         // Check whether all values are there
#         for (int key = kvp.Key - 1; key <= kvp.Key + 1; key++)
#             if (!headPositionsY.TryGet(key).HasValue)
#                 goto outerLoopEnding;

#         // Is it a local minimum?
#         if (kvp.Value < headPositionsY[kvp.Key - 1] && kvp.Value < headPositionsY[kvp.Key + 1])
#             minimaKeys.Add(kvp.Key);
#         outerLoopEnding : ;
#     }  

#     // II. Calclulate the time in between the local minima
#     if (minimaKeys.Count < 2) return null; // We cannot calc distance if there are less than 2 points
#     List<float> timeInBetween = new();
#     for (int i = 0; i < minimaKeys.Count - 1; i++)
#         timeInBetween.Add(timestamps[minimaKeys[i + 1]] - timestamps[minimaKeys[i]]);
    
#     // III. Calculate mean and convert from ms to s
#     float meanSecInBetween = 0.001f * timeInBetween.Average();

#     return 60f / meanSecInBetween; // BPM = 60 sec * (1 / meanSecInBetween);
# }

import sys
import pandas as pd
import numpy as np
import math
data = pd.DataFrame()
for arg in sys.argv:
    if arg == sys.argv[0]:
        continue
    file_path = arg
    new_data = pd.read_csv(file_path)   
    data = pd.concat([data, new_data], ignore_index=True)
pd.set_option('display.max_colwidth', None)
data['TargetSize'] = data['TargetSize'].astype('category')
data['ReferenceFrame'] = data['ReferenceFrame'].astype('category')
data['Movement'] = data['Movement'].astype('category')
data['CircleDirection'] = data['CircleDirection'].astype('category')

""" grouped = data.groupby(['ParticipantID','Movement', 'ReferenceFrame', 'TargetSize', 'CircleDirection'], dropna=False)
print(grouped.size().to_string()) """

number_repr = data.copy()
number_repr['TargetSize'] = number_repr['TargetSize'].cat.codes
number_repr['ReferenceFrame'] = number_repr['ReferenceFrame'].cat.codes
number_repr['Movement'] = number_repr['Movement'].cat.codes
number_repr = number_repr[['ParticipantID', 'Movement', 'ReferenceFrame', 'TargetSize', 'ActiveTargetIndex']]
number_repr_one = number_repr.iloc[1:].reset_index(drop=True)
number_repr_two = number_repr.iloc[:-1].reset_index(drop=True)
changes = (number_repr_one - number_repr_two).where(lambda x: x != 0).dropna(how='all')
changes.loc[len(data)-1] = number_repr_two.iloc[-1]
changes = changes != np.nan
edges = number_repr[changes].dropna()

if edges.shape != edges.drop_duplicates().shape:
    print("ERR: There are duplicates, please check the data")
    sys.exit()


# Compute step frequency
filtered_data = data[data['Movement'].isin(['Circle', 'Walking'])] # Remove Standing
local_min_vals = filtered_data.loc[filtered_data['HeadPositionY'] == filtered_data['HeadPositionY'].rolling(5, center=True).min()] # 
time_between_steps = local_min_vals['RealtimeSinceStartupMs'].diff()

# Discard steps that were faster than 50ms and slower than 2sec.
filtered_time_between_steps = list(filter(lambda x: 50 <= x <= 2000, time_between_steps.tolist()))
mean_time_between_steps_in_seconds = np.mean(filtered_time_between_steps) * 0.001
mean_step_frequency = 60 / mean_time_between_steps_in_seconds

# print(filtered_time_between_steps)
print(mean_step_frequency)

print(data.head(100))

