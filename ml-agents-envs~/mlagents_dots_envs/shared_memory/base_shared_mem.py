from abc import ABC
import os
import tempfile
import mmap
import numpy as np
import struct
import uuid
from typing import Tuple


class BasedSharedMem(ABC):
    DIRECTORY = "ml-agents"

    def __init__(self, file_name: str, create_file: bool = False, size: int = 0):
        directory = os.path.join(tempfile.gettempdir(), self.DIRECTORY)
        if not os.path.exists(directory):
            os.makedirs(directory)
        file_path = os.path.join(directory, file_name)
        if create_file:
            if os.path.exists(file_path):
                os.remove(file_path)
            data = bytearray(size)
            with open(file_path, "w+b") as f:
                f.write(data)
        with open(file_path, "r+b") as f:
            # memory-map the file, size 0 means whole file
            self.accessor = mmap.mmap(f.fileno(), 0)
        self._file_path = file_path

    def get_int(self, offset: int) -> Tuple[int, int]:
        return struct.unpack_from("<i", self.accessor, offset)[0], offset + 4

    def get_float(self, offset: int) -> Tuple[float, int]:
        return struct.unpack_from("<f", self.accessor, offset)[0], offset + 4

    def get_bool(self, offset: int) -> Tuple[bool, int]:
        return struct.unpack_from("<?", self.accessor, offset)[0], offset + 1

    def get_string(self, offset: int) -> Tuple[str, int]:
        string_len = struct.unpack_from("<B", self.accessor, offset)[0]
        byte_array = bytes(self.accessor[offset + 1 : offset + string_len + 1])
        result = byte_array.decode("ascii")
        return result, offset + string_len + 1

    def get_ndarray(
        self, offset: int, shape: Tuple[int, ...], t: np.dtype
    ) -> np.ndarray:
        count = np.prod(shape)
        return np.frombuffer(
            buffer=self.accessor, dtype=t, count=count, offset=offset
        ).reshape(shape)

    def get_uuid(self, offset: int) -> Tuple[uuid.UUID, int]:
        return uuid.UUID(bytes_le=self.accessor[offset : offset + 16]), offset + 16

    def set_int(self, offset: int, value: int) -> int:
        struct.pack_into("<i", self.accessor, offset, value)
        return offset + 4

    def set_float(self, offset: int, value: float) -> int:
        struct.pack_into("<f", self.accessor, offset, value)
        return offset + 4

    def set_bool(self, offset: int, value: bool) -> int:
        struct.pack_into("<?", self.accessor, offset, value)
        return offset + 1

    def set_string(self, offset: int, value: str) -> int:
        string_len = len(value)
        struct.pack_into("<B", self.accessor, offset, string_len)
        offset += 1
        self.accessor[offset : offset + string_len] = value.encode("ascii")
        return offset + string_len

    def set_uuid(self, offset: int, value: uuid.UUID) -> int:
        self.accessor[offset : offset + 16] = value.bytes_le
        return offset + 16

    def set_ndarray(self, offset: int, data: np.ndarray) -> None:
        bytes_data = data.tobytes()
        self.accessor[offset : offset + len(bytes_data)] = bytes_data

    def close(self) -> None:
        if self.accessor is not None:
            self.accessor.close()
            self.accessor = None  # type: ignore

    def delete(self) -> None:
        self.close()
        try:
            os.remove(self._file_path)
        except BaseException:
            pass
