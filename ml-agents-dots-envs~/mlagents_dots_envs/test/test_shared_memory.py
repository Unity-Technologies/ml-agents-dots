import os
import tempfile
import numpy as np
from mlagents_dots_envs.shared_memory.base_shared_memory import BaseSharedMemory


def test_shared_memory():
    file_path = os.path.join(tempfile.gettempdir(), "ml-agents", "test")

    sm0 = BaseSharedMemory("test", True, 256)
    sm1 = BaseSharedMemory("test", False)

    new_offset = sm0.set_int(0, 3)
    val, off = sm1.get_int(0)
    assert off == new_offset
    assert val == 3

    new_offset = sm0.set_float(0, 3)
    val, off = sm1.get_float(0)
    assert off == new_offset
    assert val == 3

    new_offset = sm0.set_bool(0, True)
    val, off = sm1.get_bool(0)
    assert off == new_offset
    assert val

    sm0.set_string(0, "foo")
    assert sm1.get_string(0)[0] == "foo"
    off = sm1.set_string(0, "bar")
    assert sm0.get_string(0)[0] == "bar"
    assert off == sm0.get_string(0)[1]

    src = np.array([1, 2, 3, 4], dtype=np.float32)
    sm0.set_ndarray(0, src)
    dst = sm1.get_ndarray(0, (4,), np.float32)
    assert np.array_equal(src, dst)

    sm0.close()
    assert os.path.exists(file_path)
    sm1.delete()
    assert not os.path.exists(file_path)
