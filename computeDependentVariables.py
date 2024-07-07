import sys
import pandas as pd
import numpy as np
import glob
import os
import math
import matplotlib.pyplot as plt
import seaborn as sns

data = pd.DataFrame()

# Download data from onedrive and set the directory containing the CSV files
directory = sys.argv[1]
shouldUseStoredValues = False
if len(sys.argv) > 2:
    shouldUseStoredValues = sys.argv[2]

# Specify the range of participants to include
participant_start = 5
participant_end = 28
for participant_id in range(participant_start - 1, participant_end + 1):
    file_pattern = os.path.join(directory, f'{participant_id}_highFrequency.csv')
    files = glob.glob(file_pattern)
    for file_path in files:
        if os.path.basename(file_path) == f'{participant_id}_highFrequency.csv':
            new_data = pd.read_csv(file_path)
            data = pd.concat([data, new_data], ignore_index=True)

storedDataFileName = 'allData.csv'
existing_data = pd.read_csv(storedDataFileName) if shouldUseStoredValues else pd.DataFrame()

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

data['conditionID'] = (data['SystemClockTimestampMs'].diff() <= 0).cumsum()

### Participants Height
data['ParticipantHeight'] = data['HeadPositionY'] - data['TrackPositionY']

def sliding_window_dispersion(df, column_name):
    window_length_ms = 1200 # 100 bpm means 0.6 sec per step => 1.2 for two steps.
    df['index1'] = df.index
    df = df.set_index(['ParticipantID', 'ReferenceFrame', 'Movement', 'TargetSize', 'SystemClockTimestampMs'])
    df_tmp = df.sort_index()
    rolling_matrix = df.apply(lambda x: 
        df_tmp.loc[(x.name[0], x.name[1], x.name[2], x.name[3], 
                slice(x.name[4] - window_length_ms, x.name[4] + window_length_ms)), column_name], 
                axis=1)
    return rolling_matrix.values

# Example usage
data = sliding_window_dispersion(data, column_name='ParticipantHeight')

### Decline
data['Decline'] = data['ParticipantHeight'] - data['AllTargetsPositionY']

data['HandHeightAboveGround'] = data['ControllerPositionY'] - data['TrackPositionY']

data['HandRotationMeanX'] = np.mean(sliding_window_dispersion(data, column_name='ControllerForwardX'), axis=1)
data['HandRotationMeanY'] = np.mean(sliding_window_dispersion(data, column_name='ControllerForwardY'), axis=1)
data['HandRotationMeanZ'] = np.mean(sliding_window_dispersion(data, column_name='ControllerForwardZ'), axis=1)
data['AngleOFHandRotationFromMean'] = np.arccos(
    np.dot(
        np.array([data['ControllerForwardX'], data['ControllerForwardY'], data['ControllerForwardZ']]).T,
        np.array([data['HandRotationMeanX'], data['HandRotationMeanY'], data['HandRotationMeanZ']]).T
    ) /
    (np.linalg.norm(np.array([data['ControllerForwardX'], data['ControllerForwardY'], data['ControllerForwardZ']], dtype=np.float64), axis=0) * np.linalg.norm(np.array([data['HandRotationMeanX'], data['HandRotationMeanY'], data['HandRotationMeanZ']], dtype=np.float64), axis=0))
)
print(data.head())
exit()

data['InFrontOfHeadPathX'] = data['WalkingDirectionPositionX'] + data['WalkingDirectionForwardX'] * 0.3
data['InFrontOfHeadPathZ'] = data['WalkingDirectionPositionZ'] + data['WalkingDirectionForwardZ'] * 0.3
norm_of_track_wd = np.linalg.norm(np.array([data['InFrontOfHeadPathX'],data['InFrontOfHeadPathZ']]) - np.array([data['TrackPositionX'], data['TrackPositionZ']]), axis=0)
data['PathWalkingDirectionX'] = np.where(
    data['Movement'] == 'Circle', 
    1.33 / norm_of_track_wd * (data['InFrontOfHeadPathX'] - data['TrackPositionX']) + data['TrackPositionX'], 
    data['InFrontOfHeadPathX']
)
data['PathWalkingDirectionZ'] = np.where(
    data['Movement'] == 'Circle',
    1.33 / norm_of_track_wd * (data['InFrontOfHeadPathZ'] - data['TrackPositionZ']) + data['TrackPositionZ'],
    data['InFrontOfHeadPathZ']
)
data['PathForwardX'] = data['PathWalkingDirectionX'] - data['WalkingDirectionPositionX']
data['PathForwardZ'] = data['PathWalkingDirectionZ'] - data['WalkingDirectionPositionZ']
normed_p_f= np.linalg.norm(data[['PathForwardX', 'PathForwardZ']], axis=1)
data['PathForwardX'] = data['PathForwardX'] / normed_p_f
data['PathForwardZ'] = data['PathForwardZ'] / normed_p_f

data = data.drop(columns=['InFrontOfHeadPathX', 'InFrontOfHeadPathZ', 'PathWalkingDirectionX', 'PathWalkingDirectionZ'])
data['DepthFromWD'] = np.linalg.norm(np.array([data['AllTargetsPositionX'] - data['WalkingDirectionPositionX'], data['AllTargetsPositionZ'] - data['WalkingDirectionPositionZ']], dtype=np.float64), axis=0)
data['HeightFromWD'] = data['AllTargetsPositionY'] - data['WalkingDirectionPositionY']
depths = data.groupby(['Movement', 'ReferenceFrame', 'ParticipantID']).agg({'DepthFromWD': 'mean', 'HeightFromWD': 'mean'}).reset_index()
depths = depths[(depths["ReferenceFrame"] == "PathReferenced") & (depths["Movement"] == "Walking")]
depths = depths.drop(columns=['Movement', 'ReferenceFrame'])
depths = depths.set_index('ParticipantID')
data['PathPositionX'] = data['WalkingDirectionPositionX'] + data['PathForwardX'] * depths['DepthFromWD'][data['ParticipantID']].reset_index(drop=True)
data['PathPositionZ'] = data['WalkingDirectionPositionZ'] + data['PathForwardZ'] * depths['DepthFromWD'][data['ParticipantID']].reset_index(drop=True)
data['PathPositionY'] = data['WalkingDirectionPositionY'] + depths['HeightFromWD'][data['ParticipantID']].reset_index(drop=True)
data['DiffFromPathX'] = data['AllTargetsPositionX'] - data['PathPositionX']
data['DiffFromPathZ'] = data['AllTargetsPositionZ'] - data['PathPositionZ']
data['DiffFromPathY'] = data['AllTargetsPositionY'] - data['PathPositionY']
data['DiffFromPath'] = np.linalg.norm(data[['DiffFromPathX', 'DiffFromPathZ', 'DiffFromPathY']], axis=1)
groupDiff = data.groupby(['ReferenceFrame','Movement']).agg({'DiffFromPath': 'std', 'DiffFromPathX': 'std', 'DiffFromPathZ': 'std', 'DiffFromPathY': 'std'}).reset_index()
print(groupDiff.to_string())

data = data[data['Movement'] != 'Standing']
sns.boxplot(data, x='Movement', y='DiffFromPath', hue='ReferenceFrame', whis=(0,100))
plt.show()

### Depth
def get_projection(row):
    head_position = np.array([row['HeadPositionX'], row['HeadPositionZ']])
    walking_forward = np.array([row['WalkingDirectionForwardX'], row['WalkingDirectionForwardZ']])
    head_to_target_vector = np.array([row['AllTargetsPositionX'] - row['HeadPositionX'], row['AllTargetsPositionZ'] - row['HeadPositionZ']])
    projection = (np.dot(head_to_target_vector, walking_forward) / np.linalg.norm(walking_forward)**2) * walking_forward
    return head_position, projection, walking_forward, head_to_target_vector

def calculate_distance_to_projection(row):
    head_position, projection, _, _ = get_projection(row)
    # Move projection vector onto head position
    parallel_line_vector = head_position + projection
    # Calculate the distance from head to the projection point
    distance_to_projection = np.linalg.norm(parallel_line_vector - head_position)
    return distance_to_projection

if not shouldUseStoredValues or 'Depth' not in existing_data.columns:
    data['Depth'] = data.apply(calculate_distance_to_projection, axis=1)
else:
    data = pd.concat([data,existing_data['Depth']], axis = 1)

### Lateral Shift
def calculate_lateral_shift(row):
    head_position, projection, walking_forward, head_to_target_vector = get_projection(row)
    # Move projections onto head position
    parallel_line_vector = head_position + projection
    head_to_target_vector = head_position + head_to_target_vector
    # Calculate the vector perpendicular to the parallel line
    perpendicular_vector = head_to_target_vector - parallel_line_vector
    distance = np.linalg.norm(perpendicular_vector)

    # Make distance negative if the targets are shiftet left, and keep positive if shiftet right
    cross_product = np.cross(np.append(walking_forward, 0), np.append(perpendicular_vector, 0))
    if cross_product[2] > 0:
        distance = -distance

    return distance

if not shouldUseStoredValues or 'LateralShift' not in existing_data.columns:
    data['LateralShift'] = data.apply(calculate_lateral_shift, axis=1)
else:
    data = pd.concat([data,existing_data['LateralShift']], axis = 1)

### Decline angle  
data['DeclineAngle'] = np.rad2deg(np.arctan(data['Decline']/data['Depth']))

### Lateral Shift Angle
data['LateralShiftAngle'] = np.rad2deg(np.arctan(data['LateralShift']/data['Depth']))

### Relative Pitch
def calculate_relative_pitch(row):
    up_vector = np.array([0, 1, 0])
    target_to_head_vector = np.array([row['HeadPositionX'] - row['AllTargetsPositionX'], row['HeadPositionY'] - row['AllTargetsPositionY'], row['HeadPositionZ'] - row['AllTargetsPositionZ']])
    target_to_head_vector /= np.linalg.norm(target_to_head_vector)
    plane_normal_vector = np.cross(target_to_head_vector, up_vector)
    plane_normal_vector /= np.linalg.norm(plane_normal_vector)
    targets_vector = -np.array([row['AllTargetsForwardX'], row['AllTargetsForwardY'], row['AllTargetsForwardZ']])
    targets_vector /= np.linalg.norm(targets_vector)
    # Projection of targets_vector onto vertical plane containing targets_to_head_vector.
    plane_projection = targets_vector - (np.dot(targets_vector, plane_normal_vector) * plane_normal_vector)
    # plane_projection = targets_vector - projection
    plane_projection /= np.linalg.norm(plane_projection)
    target_to_head_vector /= np.linalg.norm(target_to_head_vector)

    relativePitch = np.rad2deg(np.arccos(np.dot(target_to_head_vector, plane_projection)))
    horizontal_projection = np.array([target_to_head_vector[0], 0, target_to_head_vector[2]])
    horizontal_projection /= np.linalg.norm(horizontal_projection)
    absolutePitch = np.rad2deg(np.arccos(np.dot(horizontal_projection, plane_projection)))
    # If targets points higher than targets_to_head: return positive pitch, else negative
    if plane_projection[1] > target_to_head_vector[1]:
        return relativePitch, absolutePitch
    else:
        return -relativePitch, -absolutePitch

if not shouldUseStoredValues or 'RelativeTargetPitch' not in existing_data.columns or 'AbsoluteTargetPitch' not in existing_data.columns:
    data[['RelativeTargetPitch', 'AbsoluteTargetPitch']] = data.apply(calculate_relative_pitch, axis=1, result_type='expand')
else:
    data = pd.concat([data,existing_data['RelativeTargetPitch']], axis = 1)
    data = pd.concat([data,existing_data['AbsoluteTargetPitch']], axis = 1)

### Relative Yaw
def calculate_relative_yaw(row):
    up_vector = np.array([0, 1, 0])
    target_to_head_vector = np.array([row['HeadPositionX'] - row['AllTargetsPositionX'], row['HeadPositionY'] - row['AllTargetsPositionY'], row['HeadPositionZ'] - row['AllTargetsPositionZ']])
    target_to_head_vector /= np.linalg.norm(target_to_head_vector)
    temp_plane_normal_vector = np.cross(target_to_head_vector, up_vector)
    temp_plane_normal_vector /= np.linalg.norm(temp_plane_normal_vector)
    plane_normal_vector = np.cross(temp_plane_normal_vector, target_to_head_vector)
    plane_normal_vector /= np.linalg.norm(plane_normal_vector)
    targets_vector = -np.array([row['AllTargetsForwardX'], row['AllTargetsForwardY'], row['AllTargetsForwardZ']])
    targets_vector /= np.linalg.norm(targets_vector)
    # Projection of targets_vector onto vertical plane containing targets_to_head_vector.
    target_projection_on_plane = targets_vector - (np.dot(targets_vector, plane_normal_vector) * plane_normal_vector)
    target_projection_on_plane /= np.linalg.norm(target_projection_on_plane)
    backwards_walking_direction = -np.array([row['WalkingDirectionForwardX'], row['WalkingDirectionForwardY'], row['WalkingDirectionForwardZ']])
    backwards_walking_direction_projection_on_plane = backwards_walking_direction - (np.dot(backwards_walking_direction, plane_normal_vector) * plane_normal_vector)
    backwards_walking_direction_projection_on_plane /= np.linalg.norm(backwards_walking_direction_projection_on_plane)
    relativeYaw = np.rad2deg(np.arccos(np.dot(target_to_head_vector, target_projection_on_plane)))
    absoluteYaw = np.rad2deg(np.arccos(np.dot(backwards_walking_direction_projection_on_plane, target_projection_on_plane)))
    # If targets points higher than targets_to_head: return positive pitch, else negative
    direction = np.dot(target_projection_on_plane, temp_plane_normal_vector)
    if direction > 0:
        return relativeYaw, absoluteYaw
    else:
        return -relativeYaw, -absoluteYaw


if not shouldUseStoredValues or 'RelativeTargetYaw' not in existing_data.columns or 'AbsoluteTargetYaw' not in existing_data.columns:
    data[['RelativeTargetYaw', 'AbsoluteTargetYaw']] = data.apply(calculate_relative_yaw, axis=1, result_type='expand')
else:
    data = pd.concat([data,existing_data['RelativeTargetYaw']], axis = 1)
    data = pd.concat([data,existing_data['AbsoluteTargetYaw']], axis = 1)

### Relative Roll
def calculate_relative_roll(row):
    up_vector = np.array([0, 1, 0])
    target_to_head_vector = np.array([row['HeadPositionX'] - row['AllTargetsPositionX'], row['HeadPositionY'] - row['AllTargetsPositionY'], row['HeadPositionZ'] - row['AllTargetsPositionZ']])
    target_to_head_vector /= np.linalg.norm(target_to_head_vector)
    temp_plane_normal_vector = np.cross(target_to_head_vector, up_vector)
    temp_plane_normal_vector /= np.linalg.norm(temp_plane_normal_vector)
    plane_up_normal_vector = np.cross(temp_plane_normal_vector, target_to_head_vector)
    plane_up_normal_vector /= np.linalg.norm(plane_up_normal_vector)
    targets_vector = np.array([row['AllTargetsUpX'], row['AllTargetsUpY'], row['AllTargetsUpZ']])
    targets_vector /= np.linalg.norm(targets_vector)
    # Projection of targets_vector onto plane defined by tarte_to_head_vector
    target_projection_on_plane = targets_vector - (np.dot(targets_vector, target_to_head_vector) * target_to_head_vector)
    target_projection_on_plane /= np.linalg.norm(target_projection_on_plane)
    roll = np.rad2deg(np.arccos(np.dot(plane_up_normal_vector, target_projection_on_plane)))
    # If targets are rolled right, the target_projection_on_plane points right, and the direction is negative.
    direction = np.dot(target_projection_on_plane, temp_plane_normal_vector)
    if direction < 0:
        return roll
    else:
        return -roll

if not shouldUseStoredValues or 'RelativeTargetRoll' not in existing_data.columns:
    data['RelativeTargetRoll'] = data.apply(calculate_relative_roll, axis=1)
else:
    data = pd.concat([data,existing_data['RelativeTargetRoll']], axis = 1)

data = data[data['Movement'].isin(['Circle', 'Walking'])] # Remove Standing
# print(data.iloc[49000:49040])

mean_path_referenced_data = data[data['ReferenceFrame'] == 'PathReferenced']
mean_decline_depth_path = mean_path_referenced_data.groupby('ParticipantID').agg({
    'Decline': 'mean',
    'Depth': 'mean'
}).reset_index()

# Invert horizontal values for left handed people.
def invert_for_left_hand(row):
    if row['DominantHand'] == 'Left':
        row['LateralShift'] *= -1
        row['LateralShiftAngle'] *= -1
        row['RelativeTargetYaw'] *= -1
    return row

data = data.apply(invert_for_left_hand, axis=1)

if not shouldUseStoredValues:
    data.to_csv(storedDataFileName, index=False) 

result = data.groupby(['ParticipantID', 'ReferenceFrame', 'Movement', 'TargetSize']).agg(
    ParticipantID=('ParticipantID', 'first'),
    ReferenceFrame=('ReferenceFrame', 'first'),
    CircleDirection=('CircleDirection', 'first'),
    Movement=('Movement', 'first'),
    DominantHand=('DominantHand', 'first'),
    TargetSize=('TargetSize', 'first'),
    ParticipantHeight=('ParticipantHeight', 'mean'),
    ParticipantHeight_std=('ParticipantHeight_std', 'mean'),
    ParticipantHeight_range=('ParticipantHeight_range', 'mean'),
    Decline=('Decline', 'mean'),
    Depth=('Depth', 'mean'),
    LateralShift=('LateralShift', 'mean'),
    DeclineAngle=('DeclineAngle', 'mean'),
    LateralShiftAngle=('LateralShiftAngle', 'mean'),
    RelativeTargetPitch=('RelativeTargetPitch', 'mean'),
    AbsoluteTargetPitch=('AbsoluteTargetPitch', 'mean'),
    RelativeTargetYaw=('RelativeTargetYaw', 'mean'),
    AbsoluteTargetYaw=('AbsoluteTargetYaw', 'mean'),
    RelativeTargetRoll=('RelativeTargetRoll', 'mean'),
)

result = result.reset_index(drop=True)

mean_decline_depth_path = mean_decline_depth_path.rename(columns={'Decline': 'Path_mean_decline', 'Depth': 'Path_mean_depth'})
result = result.merge(mean_decline_depth_path, on='ParticipantID', how='left')
result['DeclineDiff'] = result['Decline'] - result['Path_mean_decline']
result['DepthDiff'] = result['Depth'] - result['Path_mean_depth']
result.to_csv(str(participant_start) + "-" + str(participant_end) + "_" + "additional_dependent_variables2.csv", index=False) 

# Drop the reference columns if they are no longer needed
result = result.drop(columns=['Path_mean_decline', 'Path_mean_depth'])

refs = result.groupby(['ReferenceFrame']).agg(
    ParticipantHeight=('ParticipantHeight', 'mean'),
    Decline=('Decline', 'mean'),
    Depth=('Depth', 'mean'),
    LateralShift=('LateralShift', 'mean'),
    DeclineAngle=('DeclineAngle', 'mean'),
    LateralShiftAngle=('LateralShiftAngle', 'mean'),
    RelativeTargetPitch=('RelativeTargetPitch', 'mean'),
    RelativeTargetYaw=('RelativeTargetYaw', 'mean'),
    DeclineDiff=('DeclineDiff', 'mean'),
    DepthDiff=('DepthDiff', 'mean')
)

print(refs.to_string())