import sys
import pandas as pd
import numpy as np
import glob
import os
import math

data = pd.DataFrame()

# Download data from onedrive and set the directory containing the CSV files
directory = '../Original Data'
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

### Decline
data['Decline'] = data['ParticipantHeight'] - data['AllTargetsPositionY']

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

data['Depth'] = data.apply(calculate_distance_to_projection, axis=1)

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

data['LateralShift'] = data.apply(calculate_lateral_shift, axis=1)

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

    pitch = np.rad2deg(np.arccos(np.dot(target_to_head_vector, plane_projection)))
    # If targets points higher than targets_to_head: return positive pitch, else negative
    if plane_projection[1] > target_to_head_vector[1]:
        return pitch
    else:
        return -pitch

data['RelativeTargetPitch'] = data.apply(calculate_relative_pitch, axis=1)

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
    yaw = np.rad2deg(np.arccos(np.dot(target_to_head_vector, target_projection_on_plane)))
    # If targets points higher than targets_to_head: return positive pitch, else negative
    direction = np.dot(target_projection_on_plane, temp_plane_normal_vector)
    if direction > 0:
        return yaw
    else:
        return -yaw


data['RelativeTargetYaw'] = data.apply(calculate_relative_yaw, axis=1)

filtered_data = data[data['Movement'].isin(['Circle', 'Walking'])] # Remove Standing
print(filtered_data.iloc[49000:49040])

result = filtered_data.groupby(['ParticipantID', 'ReferenceFrame']).agg(
    # ParticipantID=('ParticipantID', 'first'),
    # ReferenceFrame=('ReferenceFrame', 'first'),
    # movement=('Movement', 'first'),
    # TargetSize=('TargetSize', 'first'),
    ParticipantHeight=('ParticipantHeight', 'mean'),
    Decline=('Decline', 'mean'),
    Depth=('Depth', 'mean'),
    LateralShift=('LateralShift', 'mean'),
    DeclineAngle=('DeclineAngle', 'mean'),
    LateralShiftAngle=('LateralShiftAngle', 'mean'),
    RelativeTargetPitch=('RelativeTargetPitch', 'mean'),
    RelativeTargetYaw=('RelativeTargetYaw', 'mean'),
)

print(result.to_string())