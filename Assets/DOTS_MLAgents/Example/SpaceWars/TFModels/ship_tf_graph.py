import tensorflow as tf 
import numpy as np 
from tensorflow.python.tools import freeze_graph
import os

input_size = 7
output_size = 3
batch_size = 500

learning_rate = 0.0001


def quat_vec_mul(rot, vec):
	a1 = rot[:,0] * 2
	b2 = rot[:,1] * 2
	c3 = rot[:,2] * 2
	d4 = np.multiply(rot[:,0], a1)
	e5 = np.multiply(rot[:,1], b2)
	f6 = np.multiply(rot[:,2], c3)
	g7 = np.multiply(rot[:,0], b2)
	h8 = np.multiply(rot[:,0], c3)
	i9 = np.multiply(rot[:,1], c3)
	j10 = np.multiply(rot[:,3], a1)
	k11 = np.multiply(rot[:,3], b2)
	l12 = np.multiply(rot[:,3], c3)
	return np.transpose(np.stack((
		np.multiply(1 - e5 - f6, vec[:,0]) + np.multiply(g7 - l12, vec[:,1]) + np.multiply(h8+k11, vec[:,2]),
		np.multiply(g7+l12, vec[:,0]) + np.multiply(1- d4-f6, vec[:,1]) + np.multiply(i9-j10, vec[:,2]),
		np.multiply(h8-k11, vec[:,0]) + np.multiply(i9+j10, vec[:,1]) + np.multiply(1-d4-e5, vec[:,2])
		)))


def cross(vec1, vec2):
	x = vec1[:,0]
	y = vec1[:,1]
	z = vec1[:,2]
	a = vec2[:,0]
	b = vec2[:,1]
	c = vec2[:,2]
	return np.transpose(np.stack((
		np.multiply(y,c) - np.multiply(z,b),
		np.multiply(z,a) - np.multiply(c,x),
		np.multiply(x,b) - np.multiply(a,y) 
		)))

def dot(vec1, vec2):
	x = vec1[:,0]
	y = vec1[:,1]
	z = vec1[:,2]
	a = vec2[:,0]
	b = vec2[:,1]
	c = vec2[:,2]
	return np.transpose(np.expand_dims(
		np.multiply(x,a) + np.multiply(y,b) + np.multiply(z,c), axis = 0))

def normalize(vec):
	norm = dot(vec, vec)[:,0]
	vec[:,0] /= norm 
	vec[:,1] /= norm 
	vec[:,2] /= norm 
	return vec

assert(quat_vec_mul(np.array([[2,3,4,5]]), np.array([[6,7,8]]))[0,0] == -122)
assert(quat_vec_mul(np.array([[2,3,4,5]]), np.array([[6,7,8]]))[0,1] == 71)
assert(quat_vec_mul(np.array([[2,3,4,5]]), np.array([[6,7,8]]))[0,2] == 24)

assert(cross(np.array([[2,3,4]]), np.array([[6,7,8]]))[0,0] == -4)
assert(cross(np.array([[2,3,4]]), np.array([[6,7,8]]))[0,1] == 8)
assert(cross(np.array([[2,3,4]]), np.array([[6,7,8]]))[0,2] == -4)


forward = np.transpose(np.stack((
	np.zeros(batch_size),
	np.zeros(batch_size),
	np.ones(batch_size)
	)))

up = np.transpose(np.stack((
	np.zeros(batch_size),
	np.ones(batch_size),
	np.zeros(batch_size)
	)))

right = np.transpose(np.stack((
	np.ones(batch_size),
	np.zeros(batch_size),
	np.zeros(batch_size)
	)))


graph = tf.Graph()

with graph.as_default():
	tensor_in = tf.placeholder(shape = [None, input_size], dtype=tf.float32, name='sensor')
	hidden1 = tf.layers.dense(tensor_in, 64, activation=tf.nn.elu)
	tensor_out = tf.layers.dense(hidden1, output_size, name='why_did_you_open_the_pandora_box', activation=tf.nn.elu)
	tf.identity(tensor_out, name='actuator')

	target = tf.placeholder(shape = [None, output_size], dtype=tf.float32, name='target')

	loss = tf.reduce_mean(tf.squared_difference(target, tensor_out))
	optimizer = tf.train.AdamOptimizer(learning_rate=learning_rate)
	update_batch = optimizer.minimize(loss)

	saver = tf.train.Saver(max_to_keep=1)
	init = tf.global_variables_initializer()
	sess = tf.Session(graph=graph)
	sess.run(init)

	biased_in_data = None
	biased_target = None

	for epoch in range(100000):
		input_data = np.random.uniform(-1,1,
                size=(batch_size, input_size))
		input_data[:,:3] = normalize(input_data[:,:3] )
		pos = input_data[:,:3]
		rot = input_data[:,3:]
		fwd_rel_ref = quat_vec_mul(rot, forward)
		vec_cross = cross(pos, fwd_rel_ref)
		y_rot = dot(vec_cross, quat_vec_mul(rot, up))
		x_rot = dot(vec_cross, quat_vec_mul(rot, right))

		heuristic_target = np.transpose(np.stack((
				np.clip(y_rot, -1, 1),
				np.clip(x_rot, -1, 1),
				(dot(vec_cross, vec_cross) < 1)*1.0
			)))[0,:,:]

		firing_data_index = (heuristic_target[:, 2] > 0.5)

		for i in range(5):
			l, _ = sess.run((loss, update_batch), feed_dict = {
				tensor_in: input_data,
				target: heuristic_target
				})
			
		if epoch % 100 == 0:
			print(epoch, sum(firing_data_index), l)


	saver.save(sess, './last_checkpoint.ckpt')
	tf.train.write_graph(graph, '.', 'raw_graph_def.pb', as_text=False)
	print([x.name for x in graph.as_graph_def().node])
	freeze_graph.freeze_graph( 
		input_graph='./raw_graph_def.pb', 
		input_binary=True, 
		input_checkpoint='./last_checkpoint.ckpt', 
		output_node_names='actuator', 
		output_graph=('test.bytes'), 
		clear_devices=True, initializer_nodes='', 
		input_saver='', 
		restore_op_name='save/restore_all', 
		filename_tensor_name='save/Const:0')


