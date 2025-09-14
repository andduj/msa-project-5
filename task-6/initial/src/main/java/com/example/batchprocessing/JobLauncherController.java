package com.example.batchprocessing;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.batch.core.Job;
import org.springframework.batch.core.JobParametersBuilder;
import org.springframework.batch.core.launch.JobLauncher;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.time.Instant;

@RestController
@RequestMapping("/api/jobs")
public class JobLauncherController {

	private static final Logger log = LoggerFactory.getLogger(JobLauncherController.class);

	private final JobLauncher jobLauncher;
	private final Job importProductJob;

	public JobLauncherController(JobLauncher jobLauncher, Job importProductJob) {
		this.jobLauncher = jobLauncher;
		this.importProductJob = importProductJob;
	}

	@PostMapping("/import-products")
	public ResponseEntity<String> runJob() throws Exception {
		var params = new JobParametersBuilder()
				.addLong("ts", Instant.now().toEpochMilli())
				.toJobParameters();
		log.info("Triggering job with params {}", params);
		jobLauncher.run(importProductJob, params);
		return ResponseEntity.accepted().body("Job started");
	}
}


