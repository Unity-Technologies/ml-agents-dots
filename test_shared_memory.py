from shared_memory_communicator import (
    SharedMemoryCom,
    PythonCommand,
    AgentGroupFileOffsets,
)
import os
import tempfile
import shutil
import mmap
import struct
import numpy as np


def get_command(acc):
    return struct.unpack_from("<B", acc, SharedMemoryCom.COMMAND_OFFSET)[0]


def set_command(acc, command):
    return struct.pack_into("<B", acc, SharedMemoryCom.COMMAND_OFFSET, command)


def get_mutex(acc):
    return struct.unpack_from("<?", acc, SharedMemoryCom.MUTEXT_OFFSET)[0]


def give_python_control(acc):
    struct.pack_into("<?", acc, SharedMemoryCom.MUTEXT_OFFSET, False)


def test_create_shared_memory():
    sm = SharedMemoryCom(False)
    with open(sm.get_file_name(), "r+b") as f:
        accessor = mmap.mmap(f.fileno(), 0)
    assert len(accessor) == 18  # The size of an initialization file
    first_file_path = sm.get_file_name()
    assert os.path.exists(first_file_path)  # Ensure the file was created
    os.remove(first_file_path)
    accessor.close()


def test_file_expansion():
    sm = SharedMemoryCom(False)
    with open(sm.get_file_name(), "r+b") as f:
        accessor = mmap.mmap(f.fileno(), 0)
    first_file_path = sm.get_file_name()
    sm.write_side_channel_data(struct.pack("<i", 2019))
    # Not enough space on the file to write, creates a new file and flags the old as obsolete
    data = sm.read_side_channel_data()
    value = struct.unpack("<i", data)[0]
    assert value == 2019
    assert sm.get_file_name()[-1] == "_"
    assert get_command(accessor) == PythonCommand.CHANGE_FILE
    assert get_mutex(accessor)
    os.remove(first_file_path)
    accessor.close()
    with open(sm.get_file_name(), "r+b") as f:
        accessor = mmap.mmap(f.fileno(), 0)
    assert get_command(accessor) == PythonCommand.DEFAULT
    assert not get_mutex(accessor)
    sm.close()
    assert get_mutex(accessor)
    assert get_command(accessor) == PythonCommand.CLOSE
    accessor.close()
    os.remove(sm.get_file_name())


def test_unity_moved_file():
    sm = SharedMemoryCom(False)
    with open(sm.get_file_name(), "r+b") as f:
        accessor = mmap.mmap(f.fileno(), 0)
    first_file_path = sm.get_file_name()
    sm.give_unity_control(PythonCommand.DEFAULT)
    second_file_path = first_file_path + "_"
    shutil.copyfile(first_file_path, second_file_path)
    with open(second_file_path, "r+b") as f:
        accessor2 = mmap.mmap(f.fileno(), 0)
    set_command(accessor, PythonCommand.CHANGE_FILE)
    give_python_control(accessor)
    accessor.close()
    set_command(accessor2, PythonCommand.DEFAULT)
    give_python_control(accessor2)
    sm.wait_for_unity()
    assert sm.get_file_name() == second_file_path
    assert not os.path.exists(first_file_path)
    sm.close()
    accessor2.close()
    os.remove(second_file_path)


def test_unity_close():
    sm = SharedMemoryCom(False)
    with open(sm.get_file_name(), "r+b") as f:
        accessor = mmap.mmap(f.fileno(), 0)
    first_file_path = sm.get_file_name()
    sm.give_unity_control(PythonCommand.DEFAULT)
    set_command(accessor, PythonCommand.CLOSE)
    give_python_control(accessor)
    accessor.close()
    sm.wait_for_unity()
    assert not sm.active
    assert not os.path.exists(first_file_path)


def test_get_values():
    sm = SharedMemoryCom(False)
    with open(sm.get_file_name(), "r+b") as f:
        accessor = mmap.mmap(f.fileno(), 0)
    struct.pack_into("<i", accessor, 14, 2019)
    assert sm._get_int(accessor, 14)[0] == 2019
    struct.pack_into("<?", accessor, 14, True)
    val, offset = sm._get_bool(accessor, 14)
    assert val
    assert offset == 15
    f = 2.3
    struct.pack_into("<f", accessor, 14, f)
    val, offset = sm._get_float(accessor, 14)
    assert np.abs(val - f) < 0.001
    assert offset == 18
    accessor.close()
    os.remove(sm.get_file_name())
