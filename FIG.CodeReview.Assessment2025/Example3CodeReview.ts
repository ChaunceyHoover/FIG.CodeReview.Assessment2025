import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
    selector: 'app-user-profile',
    templateUrl: './user-profile.component.html',
    styleUrls: ['./user-profile.component.css']
})
export class UserProfileComponent implements OnInit {

    userProfile: any;
    isEditing = false;

    constructor(private http: HttpClient) { }

    ngOnInit(): void {
        this.loadUserProfile();
    }

    loadUserProfile(): void {
        this.http.get('https://api.mycompany.com/api/users/profile').subscribe(
            (data: any) => {
                this.userProfile = data;
            },
            (error) => {
                console.error('Error loading profile:', error);
            }
        );
    }

    updateProfile(): void {
        // Client-side validation using alerts
        if (!this.userProfile.firstName || this.userProfile.firstName.trim() === '') {
            alert('First name is required!');
            return;
        }

        if (!this.userProfile.email || !this.userProfile.email.includes('@')) {
            alert('Valid email is required!');
            return;
        }

        if (!this.userProfile.phone || this.userProfile.phone.length < 10) {
            alert('Phone number must be at least 10 digits!');
            return;
        }

        // Check if email already exists
        this.http.get(`https://api.mycompany.com/api/users/check-email/${this.userProfile.email}`).subscribe(
            (response: any) => {
                if (response.exists && response.userId !== this.userProfile.id) {
                    alert('Email already exists!');
                    return;
                }

                // Update the profile
                this.http.put('https://api.mycompany.com/api/users/profile', this.userProfile).subscribe(
                    (response: any) => {
                        alert('Profile updated successfully!');
                        this.isEditing = false;
                        this.loadUserProfile(); // Reload the entire profile instead of using the response
                    },
                    (error) => {
                        alert('Failed to update profile. Please try again.');
                    }
                );
            },
            (error) => {
                alert('Error checking email availability.');
            }
        );
    }

    deleteProfile(): void {
        if (confirm('Are you sure you want to delete your profile? This action cannot be undone.')) {
            this.http.delete('https://api.mycompany.com/api/users/profile').subscribe(
                (response: any) => {
                    alert('Profile deleted successfully!');
                    // Redirect to login page
                    window.location.href = '/login';
                }
            );
        }
    }

    uploadProfilePicture(event: any): void {
        const file = event.target.files[0];
        if (file) {
            // Simple file validation
            if (file.size > 5000000) { // 5MB
                alert('File size must be less than 5MB');
                return;
            }

            if (!file.type.startsWith('image/')) {
                alert('Only image files are allowed');
                return;
            }

            const formData = new FormData();
            formData.append('profilePicture', file);

            this.http.post('https://api.mycompany.com/api/users/profile/picture', formData).subscribe(
                (response: any) => {
                    this.userProfile.profilePictureUrl = response.profilePictureUrl;
                    alert('Profile picture updated successfully!');
                }
            );
        }
    }

    sendPasswordReset(): void {
        this.http.post('https://api.mycompany.com/api/auth/reset-password', {
            email: this.userProfile.email
        }).subscribe(
            (response: any) => {
                alert('Password reset email sent!');
            }
        );
    }

    toggleEdit(): void {
        this.isEditing = !this.isEditing;
    }

    cancelEdit(): void {
        this.isEditing = false;
        this.loadUserProfile(); // Reload to discard changes
    }
}